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
        actor.Send(new Message(EmptyActor.Instance, "Go"));
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
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_requestDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(timeManager.CurrentMilliseconds >= 5000);
    }

    [Fact]
    public void DataChangedTest()
    {
        var systemConfigurations = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
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
        actor.Send(new ActorItemMessage(uiActor, "/Cyl", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl" }
                && (bool)actorItemMessage.DictPayload!["On"]! == false
                && (bool)actorItemMessage.DictPayload!["IsOn"]! == false
                && !(bool)actorItemMessage.DictPayload!["InputOffValue"]!
                && !(bool)actorItemMessage.DictPayload!["InputOnValue"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && (bool)actorItemMessage.DictPayload!["IsOn"]!
                && !(bool)actorItemMessage.DictPayload!["InputOffValue"]!
                && (bool)actorItemMessage.DictPayload!["InputOnValue"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var systemConfigurations = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
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
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Cyl",
                "_itemDataWrite",
                new Dictionary<string, object?> { ["On"] = true }
            )
        );
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && !(bool)actorItemMessage.DictPayload!["IsOn"]!
                && !(bool)actorItemMessage.DictPayload!["InputOffValue"]!
                && !(bool)actorItemMessage.DictPayload!["InputOnValue"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void OnAndOffTest()
    {
        var systemConfigurations = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
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
        actor.Send(new Message(EmptyActor.Instance, "OnAndOff"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.False(actor.Cyl.On);
        Assert.True(actor.Cyl.IsOff);
        Assert.True(actor.Cyl.InputOffValue);
        Assert.False(actor.Cyl.InputOnValue);
    }

    public class TestActor : Actor
    {
        public IDoubleActingActuator Cyl;

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

            Cyl = new DoubleActingActuator(CylFwd, CylBwd, CylFwdDet, CylBwdDet);
        }

        protected override void ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "Go":
                    try
                    {
                        Cyl.OnAndWait(5000);
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }

                    break;
                case "OnAndOff":
                    Cyl.OnAndWait(5000);
                    Cyl.OffAndWait(5000);

                    break;
            }

            base.ProcessMessage(message);
        }
    }
}
