using System;
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
        var input = new FakeDigitalInput(systemConfigurations, new EmptyScenarioFlowTester())
        {
            On = true,
        };
        using var timeManager = new FrozenTimeManager();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                new EmptyVariableManager(),
                timeManager
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
        var input = new FakeDigitalInput(systemConfigurations, new EmptyScenarioFlowTester());
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
            new EmptyScenarioFlowTester()
        );
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            new EmptyAxisFactory(),
            digitalInputFactory,
            EmptyDigitalOutputFactory.Instance,
            new EmptyVariableManager(),
            timeManager,
            actorRegistry
        );
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(Actor.Empty, "go"));
        actor.Send(new Message(Actor.Empty, "_terminate"));
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
            new EmptyAxisFactory(),
            digitalInputFactory,
            EmptyDigitalOutputFactory.Instance,
            new EmptyVariableManager(),
            timeManager,
            actorRegistry
        );
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("myActor");
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
        actor.Send(new Message(Actor.Empty, "go"));
        actor.Send(new Message(Actor.Empty, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
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
            if (message.Name == "go")
                try
                {
                    MySensor.WaitOn(5000);
                }
                catch (TimeoutError)
                {
                    // Alert trigger will be checked.
                }

            base.ProcessMessage(message);
        }
    }
}
