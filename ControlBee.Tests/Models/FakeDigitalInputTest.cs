using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalInput))]
public class FakeDigitalInputTest : ActorFactoryBase
{
    [Fact]
    public void WaitOnTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };
        var input = new FakeDigitalInput(systemConfigurations, EmptyScenarioFlowTester.Instance)
        {
            On = true,
        };
        var actor = ActorFactory.Create<Actor>("MyActor");
        input.Actor = actor;
        input.WaitOn();
    }

    [Fact]
    public void SkipWaitSensorTest()
    {
        var systemConfigurations = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
        var input = new FakeDigitalInput(systemConfigurations, EmptyScenarioFlowTester.Instance);
        input.WaitOn();
        Assert.True(input.IsOn());
        input.WaitOff();
        Assert.False(input.IsOn());
    }

    [Fact]
    public void IsOnOrValueTest()
    {
        var systemConfigurations = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
        var input = new FakeDigitalInput(systemConfigurations, EmptyScenarioFlowTester.Instance);
        Assert.True(input.IsOnOrTrue());
        Assert.False(input.IsOnOrFalse());
        Assert.True(input.IsOffOrTrue());
        Assert.False(input.IsOffOrFalse());
    }

    [Fact]
    public void TimeoutTest()
    {
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void WaitOnWithDelayTest()
    {
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");
        ScenarioFlowTester.Setup(
            [
                [
                    // ReSharper disable once AccessToDisposedClosure
                    new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.MySensor).On = true),
                ],
            ]
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
    }

    [Fact]
    public void InjectPropertiesTest()
    {
        SystemPropertiesDataSource.ReadFromString(
            @"
MyActor:
  MySensor:
    Name: My Sensor
    Desc: The description describing what my sensor is.
"
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Sensor", actor.MySensor.Name);
        Assert.Equal("The description describing what my sensor is.", actor.MySensor.Desc);
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/MySensor", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "ChangeFakeDigitalInputValue"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MySensor" }
                && (bool)payload["IsOn"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MySensor" }
                && (bool)payload["IsOn"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    private class TestActor : Actor
    {
        public readonly IDigitalInput MySensor = new DigitalInputPlaceholder();

        public TestActor(ActorConfig config)
            : base(config) { }

        protected override void MessageHandler(Message message)
        {
            switch (message.Name)
            {
                case "go":
                    try
                    {
                        MySensor.WaitOn();
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }

                    break;
                case "ChangeFakeDigitalInputValue":
                    ((FakeDigitalInput)MySensor).On = true;
                    break;
            }

            base.MessageHandler(message);
        }
    }
}
