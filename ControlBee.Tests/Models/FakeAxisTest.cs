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
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeAxis))]
public class FakeAxisTest : ActorFactoryBase
{
    [Fact]
    public void MoveTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.Move(10.0);
        fakeAxis.IsMoving().Should().BeTrue();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
        Mock.Get(scenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.Once);

        Mock.Get(scenarioFlowTester).Invocations.Clear();
        fakeAxis.Wait();
        fakeAxis.IsMoving().Should().BeFalse();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
        Mock.Get(scenarioFlowTester).Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(AxisDirection.Positive)]
    [InlineData(AxisDirection.Negative)]
    public void VelocityMoveTest(AxisDirection direction)
    {
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;
        using var frozenTimeManager = new FrozenTimeManager(
            new FrozenTimeManagerConfig { ManualMode = true },
            scenarioFlowTester
        );

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.VelocityMove(direction);
        fakeAxis.IsMoving().Should().BeTrue();
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
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.Move(10.0);
        scenarioFlowTesterMock.Invocations.Clear();
        fakeAxis.Wait();
        fakeAxis.IsMoving().Should().BeFalse();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
        scenarioFlowTesterMock.Verify(m => m.OnCheckpoint(), Times.AtLeastOnce);
    }

    [Fact]
    public void MoveWithoutSpeedProfileTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(timeManager, scenarioFlowTester);
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

        var fakeAxis = new FakeAxis(timeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 0.0 });
        var action = () => fakeAxis.Move(10.0);
        action
            .Should()
            .Throw<ValueError>()
            .WithMessage("You must provide a speed greater than 0 to move the axis.");
    }

    [Fact]
    public void StopTest()
    {
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;
        using var frozenTimeManager = new FrozenTimeManager(
            new FrozenTimeManagerConfig { ManualMode = true },
            scenarioFlowTester
        );

        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.VelocityMove(AxisDirection.Positive);

        frozenTimeManager.Tick(100);
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.1);

        scenarioFlowTesterMock.Invocations.Clear();
        fakeAxis.Stop();
        fakeAxis.IsMoving().Should().BeFalse();
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
        var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTesterMock = new Mock<IScenarioFlowTester>();
        var scenarioFlowTester = scenarioFlowTesterMock.Object;

        var fakeAxis = new FakeAxis(timeManager, scenarioFlowTester);
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

    [Fact]
    public void SkipWaitTest()
    {
        using var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();

        var fakeAxis = new FakeAxis(timeManager, scenarioFlowTester, true);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.Move(10.0);
        fakeAxis.IsMoving().Should().BeTrue();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(0.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);

        fakeAxis.Wait();
        fakeAxis.IsMoving().Should().BeFalse();
        fakeAxis.GetPosition(PositionType.Command).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Actual).Should().Be(10.0);
        fakeAxis.GetPosition(PositionType.Target).Should().Be(10.0);
    }

    [Fact]
    public void IsNearTest()
    {
        using var timeManager = Mock.Of<ITimeManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var fakeAxis = new FakeAxis(timeManager, scenarioFlowTester);
        fakeAxis.SetPosition(9.0);
        Assert.True(fakeAxis.IsNear(10.0, 1.0));
        Assert.False(fakeAxis.IsNear(11.0, 1.0));
    }

    [Fact]
    public void WaitForPositionTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );

        frozenTimeManager.Register();
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
        fakeAxis.Move(10.0);
        fakeAxis.WaitForPosition(PositionComparisonType.Greater, 5);
        Assert.True(fakeAxis.GetPosition(PositionType.Command) is > 5 and < 6);
        fakeAxis.Wait();
        Assert.Equal(10, fakeAxis.GetPosition(PositionType.Command));
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
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
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
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);
        fakeAxis.SetSpeed(new SpeedProfile { Velocity = 10.0 });
        fakeAxis.Move(10.0);
        Assert.Throws<PlatformException>(
            () => fakeAxis.WaitForPosition(PositionComparisonType.Greater, 20)
        );
    }
}
