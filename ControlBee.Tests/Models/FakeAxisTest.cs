using System;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using MathNet.Numerics.RootFinding;
using Moq;
using Xunit;

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
                actor.X.IsMoving().Should().BeTrue();
                actor.X.GetPosition().Should().Be(0.0);
                actor.X.GetPosition(PositionType.Actual).Should().Be(0.0);
                actor.X.GetPosition(PositionType.Target).Should().Be(10.0);
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
                actor.X.IsMoving().Should().BeFalse();
                actor.X.GetPosition().Should().Be(10.0);
                actor.X.GetPosition(PositionType.Actual).Should().Be(10.0);
                actor.X.GetPosition(PositionType.Target).Should().Be(10.0);
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
                actor.X.IsMoving().Should().BeTrue();
                actor.X.GetPosition().Should().Be(0.0);
                actor.X.GetPosition(PositionType.Actual).Should().Be(0.0);
                Assert.True(((FakeAxis)actor.X).IsMovingMonitored);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        actor
                            .X.GetPosition(PositionType.Target)
                            .Should()
                            .Be(double.PositiveInfinity);
                        break;
                    case AxisDirection.Negative:
                        actor
                            .X.GetPosition(PositionType.Target)
                            .Should()
                            .Be(double.NegativeInfinity);
                        break;
                    default:
                        throw new Exception();
                }

                Mock.Get(scenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);

                timeManager.Tick(100);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        actor.X.GetPosition().Should().Be(0.1);
                        actor.X.GetPosition(PositionType.Actual).Should().Be(0.1);
                        break;
                    case AxisDirection.Negative:
                        actor.X.GetPosition().Should().Be(-0.1);
                        actor.X.GetPosition(PositionType.Actual).Should().Be(-0.1);
                        break;
                    default:
                        throw new Exception();
                }

                timeManager.Tick(100);
                switch (direction)
                {
                    case AxisDirection.Positive:
                        actor.X.GetPosition().Should().Be(0.2);
                        actor.X.GetPosition(PositionType.Actual).Should().Be(0.2);
                        break;
                    case AxisDirection.Negative:
                        actor.X.GetPosition().Should().Be(-0.2);
                        actor.X.GetPosition(PositionType.Actual).Should().Be(-0.2);
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
                actor.X.IsMoving().Should().BeFalse();
                actor.X.GetPosition().Should().Be(10.0);
                actor.X.GetPosition(PositionType.Actual).Should().Be(10.0);
                actor.X.GetPosition(PositionType.Target).Should().Be(10.0);
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
        var action = () => fakeAxis.Move(10.0);
        action
            .Should()
            .Throw<ValueError>()
            .WithMessage("You need to provide a SpeedProfile to move the axis.");
    }

    [Fact]
    public void MoveWithZeroSpeedTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(DeviceManager, timeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 0.0 });
        var action = () => fakeAxis.Move(10.0);
        action
            .Should()
            .Throw<ValueError>()
            .WithMessage("You must provide a speed greater than 0 to move the axis.");
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
                fakeAxis.GetSensorValue(AxisSensorType.Home).Should().BeTrue();
                break;
            case AxisSensorType.PositiveLimit:
                fakeAxis.GetSensorValue(AxisSensorType.PositiveLimit).Should().BeTrue();
                break;
            case AxisSensorType.NegativeLimit:
                fakeAxis.GetSensorValue(AxisSensorType.NegativeLimit).Should().BeTrue();
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
            Assert.IsNotType<ErrorState>(actor.State);
        }
        else
        {
            Assert.IsType<ErrorState>(actor.State);
            Assert.IsType<TimeoutError>(((ErrorState)actor.State).Error);
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
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 10.0 });
        fakeAxis.Move(10.0);
        Assert.Throws<SequenceError>(
            () => fakeAxis.WaitForPosition(PositionComparisonType.Greater, 20)
        );
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
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 10.0 });
        fakeAxis.Move(10.0);
        fakeAxis.WaitForPosition(PositionComparisonType.GreaterOrEqual, 10);
    }

    private class TestActor : Actor
    {
        public IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "WaitSensor":
                    X.WaitSensor(AxisSensorType.Home, true, 1000);
                    return true;
                case "EnableX":
                    X.Enable(true);
                    message.Sender.Send(new Message(message, this, "EnableXDone"));
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

    [Fact]
    public void RefreshCacheTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);

        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(uiActor, "EnableX"));
        actor.Send(new TerminateMessage());
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && (bool)actorItemMessage.DictPayload!["IsEnabled"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);
        Assert.IsNotType<FatalErrorState>(actor.State);
    }
}
