using System;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeAxis))]
public class FakeAxisTest
{
    [Fact]
    public void MoveTest()
    {
        var frozenTimeManagerMock = new Mock<IFrozenTimeManager>();
        var frozenTimeManager = frozenTimeManagerMock.Object;

        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.Move(10.0);
        fakeAxis.IsMoving.Should().BeTrue();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.Once);
    }

    [Theory]
    [InlineData(AxisDirection.Positive)]
    [InlineData(AxisDirection.Negative)]
    public void VelocityMoveTest(AxisDirection direction)
    {
        var frozenTimeManager = new FrozenTimeManager(100);

        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.VelocityMove(direction);
        fakeAxis.IsMoving.Should().BeTrue();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.0);
        switch (direction)
        {
            case AxisDirection.Positive:
                fakeAxis.GetPosition(PositionType.Target).Should().Be(double.PositiveInfinity);
                break;
            case AxisDirection.Negative:
                fakeAxis.GetPosition(PositionType.Target).Should().Be(double.NegativeInfinity);
                break;
            default:
                throw new Exception();
        }
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.Once);

        frozenTimeManager.Tick(100);
        switch (direction)
        {
            case AxisDirection.Positive:
                fakeAxis.GetPosition(PositionType.Command).Should().Be(0.1);
                fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.1);
                break;
            case AxisDirection.Negative:
                fakeAxis.GetPosition(PositionType.Command).Should().Be(-0.1);
                fakeAxis.GetPosition(PositionType.Actual).Should().Be(-0.1);
                break;
            default:
                throw new Exception();
        }

        frozenTimeManager.Tick(100);
        switch (direction)
        {
            case AxisDirection.Positive:
                fakeAxis.GetPosition(PositionType.Command).Should().Be(0.2);
                fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.2);
                break;
            case AxisDirection.Negative:
                fakeAxis.GetPosition(PositionType.Command).Should().Be(-0.2);
                fakeAxis.GetPosition(PositionType.Actual).Should().Be(-0.2);
                break;
            default:
                throw new Exception();
        }
    }

    [Fact]
    public void WaitTest()
    {
        var frozenTimeManager = new FrozenTimeManager(100);

        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.Move(10.0);
        scenarioFlowTesterMock.Invocations.Clear();
        fakeAxis.Wait();
        fakeAxis.IsMoving.Should().BeFalse();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
    }

    [Fact]
    public void MoveWithoutSpeedProfileTest()
    {
        var frozenTimeManager = new FrozenTimeManager(100);
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.Move(10.0);
        var action = () => fakeAxis.MoveAndWait(10.0);
        action
            .Should()
            .Throw<ValueError>()
            .WithMessage("You need to provide a SpeedProfile to move the axis.");
    }

    [Fact]
    public void MoveWithZeroSpeedTest()
    {
        var frozenTimeManager = new FrozenTimeManager(100);
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 0.0 });
        var action = () => fakeAxis.MoveAndWait(10.0);
        action
            .Should()
            .Throw<ValueError>()
            .WithMessage("You must provide a speed greater than 0 to move the axis.");
    }

    [Fact]
    public void StopTest()
    {
        var frozenTimeManager = new FrozenTimeManager(100);
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.VelocityMove(AxisDirection.Positive);

        frozenTimeManager.Tick(100);
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.1);

        scenarioFlowTesterMock.Invocations.Clear();
        fakeAxis.Stop();
        fakeAxis.IsMoving.Should().BeFalse();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.1);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.1);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(0.1);
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.Once);
    }

    [Theory]
    [InlineData(AxisSensorType.Home)]
    [InlineData(AxisSensorType.PositiveLimit)]
    [InlineData(AxisSensorType.NegativeLimit)]
    public void SetSensorValueTest(AxisSensorType sensorType)
    {
        var frozenTimeManager = Mock.Of<IFrozenTimeManager>();
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSensorValue(sensorType, true);
        switch (sensorType)
        {
            case AxisSensorType.Home:
                fakeAxis.HomeSensor.Should().BeTrue();
                break;
            case AxisSensorType.PositiveLimit:
                fakeAxis.PositiveLimitSensor.Should().BeTrue();
                break;
            case AxisSensorType.NegativeLimit:
                fakeAxis.NegativeLimitSensor.Should().BeTrue();
                break;
        }
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.Once);
    }
}
