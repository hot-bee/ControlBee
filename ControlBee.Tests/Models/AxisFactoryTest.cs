using ControlBee.Interfaces;
using ControlBee.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(AxisFactory))]
public class AxisFactoryTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CreateTest(bool fakeMode, bool skipWait)
    {
        var systemConfiguration = new SystemConfigurations
        {
            FakeMode = fakeMode,
            SkipWaitSensor = skipWait,
        };
        var timeManager = Mock.Of<ITimeManager>();
        var deviceManager = Mock.Of<IDeviceManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var deviceMonitor = Mock.Of<IDeviceMonitor>();
        var axisFactory = new AxisFactory(
            systemConfiguration,
            deviceManager,
            timeManager,
            scenarioFlowTester,
            deviceMonitor
        );

        var axis = axisFactory.Create();
        if (fakeMode)
            axis.Should().BeOfType(typeof(FakeAxis));
        else
            axis.Should().BeOfType(typeof(Axis));
    }
}
