using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalOutput))]
public class FakeDigitalOutputTest
{
    [Fact]
    public void OnOffTest()
    {
        var fakeDigitalOutput = new FakeDigitalOutput();
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = true;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = false;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = true;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = false;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);
    }

    [Fact]
    public void DataChangedTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };
        var deviceManger = Mock.Of<IDeviceManager>();
        var scenarioFlowTester = new ScenarioFlowTester();
        using var timeManager = new FrozenTimeManager(new FrozenTimeManagerConfig());
        var digitalInputFactory = new DigitalInputFactory(
            systemConfigurations,
            deviceManger,
            scenarioFlowTester
        );
        var digitalOutputFactory = new DigitalOutputFactory(systemConfigurations, deviceManger);
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            digitalInputFactory,
            digitalOutputFactory,
            EmptyInitializeSequenceFactory.Instance,
            EmptyVariableManager.Instance,
            timeManager,
            EmptyActorItemInjectionDataSource.Instance,
            actorRegistry
        );
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(uiActor);
        var actor = actorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/Vacuum", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };
        var deviceManger = Mock.Of<IDeviceManager>();
        var scenarioFlowTester = new ScenarioFlowTester();
        using var timeManager = new FrozenTimeManager(new FrozenTimeManagerConfig());
        var digitalInputFactory = new DigitalInputFactory(
            systemConfigurations,
            deviceManger,
            scenarioFlowTester
        );
        var digitalOutputFactory = new DigitalOutputFactory(systemConfigurations, deviceManger);
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            digitalInputFactory,
            digitalOutputFactory,
            EmptyInitializeSequenceFactory.Instance,
            EmptyVariableManager.Instance,
            timeManager,
            EmptyActorItemInjectionDataSource.Instance,
            actorRegistry
        );
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(uiActor);
        var actor = actorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/Vacuum", "_itemDataRead"));
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Vacuum",
                "_itemDataWrite",
                new Dictionary<string, object?> { ["On"] = true }
            )
        );
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)actorItemMessage.DictPayload!["On"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)actorItemMessage.DictPayload!["On"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    public class TestActor : Actor
    {
        public IDigitalOutput Vacuum;

        public TestActor(ActorConfig config)
            : base(config)
        {
            Vacuum = DigitalOutputFactory.Create();
        }

        protected override void ProcessMessage(Message message)
        {
            if (message.Name == "Go")
            {
                Vacuum.On = true;
                return;
            }

            base.ProcessMessage(message);
        }
    }
}
