using System;
using System.Collections.Generic;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalInput))]
public class FakeDigitalInputTest
{
    [Fact]
    public void SkipWaitOnTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };
        var input = new FakeDigitalInput(systemConfigurations, EmptyScenarioFlowTester.Instance)
        {
            On = true,
        };
        using var timeManager = new FrozenTimeManager();
        var actor = new Actor(
            new ActorConfig(
                "MyActor",
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                EmptyVariableManager.Instance,
                timeManager,
                EmptyActorItemInjectionDataSource.Instance
            )
        );
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
        input.WaitOff();
    }

    [Fact]
    public void TimeoutTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };

        var deviceManger = Mock.Of<IDeviceManager>();
        using var timeManager = new FrozenTimeManager(new FrozenTimeManagerConfig());
        var digitalInputFactory = new DigitalInputFactory(
            systemConfigurations,
            deviceManger,
            EmptyScenarioFlowTester.Instance
        );
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            digitalInputFactory,
            EmptyDigitalOutputFactory.Instance,
            EmptyVariableManager.Instance,
            timeManager,
            EmptyActorItemInjectionDataSource.Instance,
            actorRegistry
        );
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void WaitOnTest()
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
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            digitalInputFactory,
            EmptyDigitalOutputFactory.Instance,
            EmptyVariableManager.Instance,
            timeManager,
            EmptyActorItemInjectionDataSource.Instance,
            actorRegistry
        );
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("MyActor");
        scenarioFlowTester.Setup(
            [
                [
                    // ReSharper disable once AccessToDisposedClosure
                    new ConditionStep(() => timeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.MySensor).On = true),
                ],
            ]
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
    }

    [Fact]
    public void InjectPropertiesTest()
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = true };
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
  MySensor:
    Name: My Sensor
    Desc: The description describing what my sensor is.
"
        );
        var actor = actorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Sensor", actor.MySensor.Name);
        Assert.Equal("The description describing what my sensor is.", actor.MySensor.Desc);
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

    public class TestActor : Actor
    {
        public IDigitalInput MySensor;

        public TestActor(ActorConfig config)
            : base(config)
        {
            MySensor = DigitalInputFactory.Create();
        }

        protected override void ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "go":
                    try
                    {
                        MySensor.WaitOn(5000);
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

            base.ProcessMessage(message);
        }
    }
}
