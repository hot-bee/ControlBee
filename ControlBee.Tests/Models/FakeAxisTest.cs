using System;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeAxis))]
public class FakeAxisTest : ActorFactoryBase
{
    [Fact]
    public void MoveTest()
    {
        Recreate(
            new ActorFactoryBaseConfig { ScenarioFlowTester = Mock.Of<IScenarioFlowTester>() }
        );
        var client = MockActorFactory.Create("client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "MoveDone",
            message =>
            {
                Assert.True(((FakeAxis)actor.X).IsMovingMonitored);
                Assert.True(actor.X.IsMoving());
                Assert.Equal(0.0, actor.X.GetPosition());
                Assert.Equal(0.0, actor.X.GetPosition(PositionType.Actual));
                Assert.Equal(10.0, actor.X.GetPosition(PositionType.Target));
                Mock.Get(ScenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
                Mock.Get(ScenarioFlowTester).Invocations.Clear();
                actor.Send(new Message(client, "Wait"));
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "WaitDone",
            message =>
            {
                Assert.False(((FakeAxis)actor.X).IsMovingMonitored);
                Assert.False(actor.X.IsMoving());
                Assert.Equal(10.0, actor.X.GetPosition());
                Assert.Equal(10.0, actor.X.GetPosition(PositionType.Actual));
                Assert.Equal(10.0, actor.X.GetPosition(PositionType.Target));
                Mock.Get(ScenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(new Message(client, "Move"));
        actor.Join();
    }

    [Fact]
    public void RelativeMoveTest()
    {
        Recreate(
            new ActorFactoryBaseConfig { ScenarioFlowTester = Mock.Of<IScenarioFlowTester>() }
        );
        var client = MockActorFactory.Create("client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(client, "RelativeMove"));
        actor.Send(new Message(client, "RelativeMove"));
        actor.Send(new Message(client, "RelativeMove"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.Equal(30.0, actor.X.GetPosition());
    }

    [Theory]
    [InlineData(AxisDirection.Positive)]
    [InlineData(AxisDirection.Negative)]
    public void VelocityMoveTest(AxisDirection direction)
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var timeManager = new FrozenTimeManager(
            new FrozenTimeManagerConfig { ManualMode = true },
            scenarioFlowTester
        );
        Recreate(
            new ActorFactoryBaseConfig
            {
                ScenarioFlowTester = scenarioFlowTester,
                TimeManager = timeManager,
            }
        );
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "VelocityMoveDone",
            message =>
            {
                Assert.True(actor.X.IsMoving());
                Assert.Equal(0.0, actor.X.GetPosition());
                Assert.Equal(0.0, actor.X.GetPosition(PositionType.Actual));
                Assert.True(((FakeAxis)actor.X).IsMovingMonitored);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        Assert.True(
                            double.IsPositiveInfinity(actor.X.GetPosition(PositionType.Target))
                        );
                        break;
                    case AxisDirection.Negative:
                        Assert.True(
                            double.IsNegativeInfinity(actor.X.GetPosition(PositionType.Target))
                        );
                        break;
                    default:
                        throw new Exception();
                }

                Mock.Get(scenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);

                timeManager.Tick(100);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        Assert.Equal(0.1, actor.X.GetPosition());
                        Assert.Equal(0.1, actor.X.GetPosition(PositionType.Actual));
                        break;
                    case AxisDirection.Negative:
                        Assert.Equal(-0.1, actor.X.GetPosition());
                        Assert.Equal(-0.1, actor.X.GetPosition(PositionType.Actual));
                        break;
                    default:
                        throw new Exception();
                }

                timeManager.Tick(100);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        Assert.Equal(0.2, actor.X.GetPosition());
                        Assert.Equal(0.2, actor.X.GetPosition(PositionType.Actual));
                        break;
                    case AxisDirection.Negative:
                        Assert.Equal(-0.2, actor.X.GetPosition());
                        Assert.Equal(-0.2, actor.X.GetPosition(PositionType.Actual));
                        break;
                    default:
                        throw new Exception();
                }

                actor.Send(new TerminateMessage());
            }
        );
        actor.Start();
        actor.Send(new Message(client, "VelocityMove", direction));
        actor.Join();
    }

    [Fact]
    public void VelocityMoveStopTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        Recreate(new ActorFactoryBaseConfig { ScenarioFlowTester = scenarioFlowTester });
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "VelocityMoveDone",
            message =>
            {
                actor.Send(new Message(client, "Stop"));
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "StopDone",
            message =>
            {
                Assert.False(((FakeAxis)actor.X).IsMovingMonitored);
                Assert.False(actor.X.IsMoving());
                Mock.Get(scenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(new Message(client, "VelocityMove", AxisDirection.Positive));
        actor.Join();
    }

    [Fact]
    public void VelocityMoveOnMovingTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        Recreate(new ActorFactoryBaseConfig { ScenarioFlowTester = scenarioFlowTester });
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        var count = 0;
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "VelocityMoveDone",
            message =>
            {
                switch (count)
                {
                    case 0:
                        Assert.True(actor.X.IsMoving());
                        Assert.Equal(
                            double.PositiveInfinity,
                            actor.X.GetPosition(PositionType.Target)
                        );
                        actor.Send(new Message(client, "VelocityMove", AxisDirection.Negative));
                        break;
                    case 1:
                        Assert.True(actor.X.IsMoving());
                        Assert.Equal(
                            double.NegativeInfinity,
                            actor.X.GetPosition(PositionType.Target)
                        );
                        actor.Send(new TerminateMessage());
                        break;
                }

                count++;
            }
        );
        actor.Start();
        actor.Send(new Message(client, "VelocityMove", AxisDirection.Positive));
        actor.Join();
    }

    [Fact]
    public void WaitTest()
    {
        Recreate(
            new ActorFactoryBaseConfig { ScenarioFlowTester = Mock.Of<IScenarioFlowTester>() }
        );
        var client = MockActorFactory.Create("client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "MoveDone",
            message =>
            {
                actor.Send(new Message(client, "Wait"));
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "WaitDone",
            message =>
            {
                Assert.False(actor.X.IsMoving());
                Assert.Equal(10.0, actor.X.GetPosition());
                Assert.Equal(10.0, actor.X.GetPosition(PositionType.Actual));
                Assert.Equal(10.0, actor.X.GetPosition(PositionType.Target));
                Mock.Get(ScenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);

                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(new Message(client, "Move"));
        actor.Join();
    }

    [Fact]
    public void MoveWithoutSpeedProfileTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(DeviceManager, timeManager, scenarioFlowTester);
        var action = Assert.Throws<ValueError>(() => fakeAxis.Move(10.0));
        Assert.Equal("You need to provide a SpeedProfile to move the axis.", action.Message);
    }

    [Fact]
    public void MoveWithZeroSpeedTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(DeviceManager, timeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 0.0 });
        var action = Assert.Throws<ValueError>(() => fakeAxis.Move(10.0));
        Assert.Equal("You must provide a speed greater than 0 to move the axis.", action.Message);
    }

    [Theory]
    [InlineData(AxisSensorType.Home)]
    [InlineData(AxisSensorType.PositiveLimit)]
    [InlineData(AxisSensorType.NegativeLimit)]
    public void SetSensorValueTest(AxisSensorType sensorType)
    {
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(DeviceManager, timeManager, scenarioFlowTester);
        fakeAxis.SetSensorValue(sensorType, true);
        switch (sensorType)
        {
            case AxisSensorType.Home:
                Assert.True(fakeAxis.GetSensorValue(AxisSensorType.Home));
                break;
            case AxisSensorType.PositiveLimit:
                Assert.True(fakeAxis.GetSensorValue(AxisSensorType.PositiveLimit));
                break;
            case AxisSensorType.NegativeLimit:
                Assert.True(fakeAxis.GetSensorValue(AxisSensorType.NegativeLimit));
                break;
        }

        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipWaitTest(bool skipWaitSensor)
    {
        Recreate(
            new ActorFactoryBaseConfig
            {
                SystemConfigurations = new SystemConfigurations
                {
                    FakeMode = true,
                    SkipWaitSensor = skipWaitSensor,
                },
            }
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "WaitSensor"));
        actor.Send(new TerminateMessage());
        actor.Join();
        if (skipWaitSensor)
        {
            Assert.IsNotType<ErrorState<TestActor>>(actor.State);
        }
        else
        {
            Assert.IsType<ErrorState<TestActor>>(actor.State);
            Assert.IsType<TimeoutError>(((ErrorState<TestActor>)actor.State).Error);
        }
    }

    [Fact]
    public void IsNearTest()
    {
        using var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var fakeAxis = new FakeAxis(DeviceManager, timeManager, scenarioFlowTester);
        fakeAxis.SetPosition(9.0);
        Assert.True(fakeAxis.IsNear(10.0, 1.0));
        Assert.False(fakeAxis.IsNear(11.0, 1.0));
    }

    [Fact]
    public void WaitForPositionTest()
    {
        Recreate(
            new ActorFactoryBaseConfig { ScenarioFlowTester = Mock.Of<IScenarioFlowTester>() }
        );
        var client = MockActorFactory.Create("client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "WaitForPositionDone",
            message =>
            {
                Assert.True(actor.X.GetPosition() is > 5 and < 6);
                actor.Send(new Message(client, "Wait"));
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "WaitDone",
            message =>
            {
                Assert.Equal(10, actor.X.GetPosition());
                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(new Message(client, "WaitForPosition"));
        actor.Join();
    }

    [Fact]
    public void WaitForPositionWithHighVelocityTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(DeviceManager, frozenTimeManager, scenarioFlowTester);
        fakeAxis.Enable(true);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 100.0 });
        fakeAxis.Move(10.0);
        fakeAxis.WaitForPosition(PositionComparisonType.Greater, 5);
        Assert.False(fakeAxis.IsMoving());
        Assert.Equal(10, fakeAxis.GetPosition(PositionType.Command));
    }

    [Fact]
    public void WaitForPositionErrorTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(DeviceManager, frozenTimeManager, scenarioFlowTester);
        fakeAxis.Enable(true);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 10.0 });
        fakeAxis.Move(10.0);
        Assert.False(fakeAxis.WaitForPosition(PositionComparisonType.Greater, 20));
    }

    [Fact]
    public void WaitForPositionStoppedTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(DeviceManager, frozenTimeManager, scenarioFlowTester);
        fakeAxis.Enable(true);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 10.0 });
        fakeAxis.Move(10.0);
        fakeAxis.WaitForPosition(PositionComparisonType.GreaterOrEqual, 10);
    }

    [Fact]
    public void RefreshCacheTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);

        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(uiActor, "DisableX"));
        actor.Send(new TerminateMessage());
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && !(bool)actorItemMessage.DictPayload!["IsEnabled"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);
        Assert.IsNotType<FatalErrorState<TestActor>>(actor.State);
    }

    private class TestActor : Actor
    {
        public readonly IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            X.Enable(true);
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<TestActor>(this, error);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "WaitSensor":
                    X.WaitSensor(AxisSensorType.Home, true, 1000);
                    return true;
                case "DisableX":
                    X.Disable();
                    message.Sender.Send(new Message(message, this, "DisableXDone"));
                    return true;
                case "Move":
                    X.SetSpeed(new SpeedProfile { Velocity = 1.0 });
                    X.Move(10.0);
                    message.Sender.Send(new Message(message, this, "MoveDone"));
                    return true;
                case "Wait":
                    X.Wait();
                    message.Sender.Send(new Message(message, this, "WaitDone"));
                    return true;
                case "RelativeMove":
                    X.SetSpeed(new SpeedProfile { Velocity = 1.0 });
                    X.RelativeMoveAndWait(10.0);
                    message.Sender.Send(new Message(message, this, "RelativeMoveDone"));
                    return true;
                case "WaitForPosition":
                    X.SetSpeed(new SpeedProfile { Velocity = 1.0 });
                    X.Move(10.0);
                    X.WaitForPosition(PositionComparisonType.Greater, 5);
                    message.Sender.Send(new Message(message, this, "WaitForPositionDone"));
                    return true;
                case "VelocityMove":
                {
                    var direction = (AxisDirection)message.Payload!;
                    X.SetSpeed(new SpeedProfile { Velocity = 1.0 });
                    X.VelocityMove(direction);
                    message.Sender.Send(new Message(message, this, "VelocityMoveDone"));
                    return true;
                }
                case "Stop":
                    X.Stop();
                    message.Sender.Send(new Message(message, this, "StopDone"));
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }
}
