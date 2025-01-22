using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(EmptyActorItem))]
public class EmptyActorItemTest
{
    [Fact]
    public void GetMetaDataTest()
    {
        var systemConfigurations = new SystemConfigurations();
        var actorItemInjectionDataSource = new ActorItemInjectionDataSource();
        var deviceManger = Mock.Of<IDeviceManager>();
        var scenarioFlowTester = new ScenarioFlowTester();
        using var timeManager = new FrozenTimeManager(new FrozenTimeManagerConfig());
        var digitalInputFactory = new DigitalInputFactory(
            systemConfigurations,
            deviceManger,
            scenarioFlowTester
        );
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            digitalInputFactory,
            EmptyDigitalOutputFactory.Instance,
            EmptyVariableManager.Instance,
            timeManager,
            actorItemInjectionDataSource,
            actorRegistry
        );
        actorItemInjectionDataSource.ReadFromString(
            @"
MyActor:
  EmptyItem:
    Name: My Empty Item
    Desc: The description describing what my empty item is.
"
        );
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(uiActor);
        var actor = actorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Empty Item", actor.EmptyItem.Name);
        Assert.Equal("The description describing what my empty item is.", actor.EmptyItem.Desc);

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/EmptyItem", "_itemMetaDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
        {
            var metaData = (Dictionary<string, object>)message.Payload!;
            return message.Name == "_itemMetaData"
                && metaData["Name"] as string == "My Empty Item"
                && metaData["Desc"] as string
                    == "The description describing what my empty item is.";
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    public class TestActor(ActorConfig config) : Actor(config)
    {
        public EmptyActorItem EmptyItem = new();
    }
}
