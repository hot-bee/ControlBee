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

[TestSubject(typeof(DoubleActingActuator))]
public class DoubleActingActuatorTest
{
    [Fact]
    public void OnAndWaitTest()
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
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("myActor");
        scenarioFlowTester.Setup(
            [
                [
                    // ReSharper disable once AccessToDisposedClosure
                    new ConditionStep(() => timeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.CylFwdDet).On = true),
                ],
            ]
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
        Assert.True(timeManager.CurrentMilliseconds is > 1000 and < 2000);
    }

    [Fact]
    public void OnAndTimeoutTest()
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
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("ui");
        actorRegistry.Add(ui);
        var actor = actorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(timeManager.CurrentMilliseconds >= 5000);
    }

    public class TestActor : Actor
    {
        public DoubleActingActuator Cyl;

        public IDigitalOutput CylBwd;
        public IDigitalInput CylBwdDet;
        public IDigitalOutput CylFwd;
        public IDigitalInput CylFwdDet;

        public TestActor(ActorConfig config)
            : base(config)
        {
            CylBwdDet = DigitalInputFactory.Create();
            CylFwdDet = DigitalInputFactory.Create();

            CylBwd = DigitalOutputFactory.Create();
            CylFwd = DigitalOutputFactory.Create();

            Cyl = new DoubleActingActuator(CylBwd, CylFwd, CylBwdDet, CylFwdDet);
        }

        protected override void ProcessMessage(Message message)
        {
            if (message.Name == "go")
                try
                {
                    Cyl.OnAndWait(5000);
                }
                catch (TimeoutError)
                {
                    // Alert trigger will be checked.
                }

            base.ProcessMessage(message);
        }
    }
}
