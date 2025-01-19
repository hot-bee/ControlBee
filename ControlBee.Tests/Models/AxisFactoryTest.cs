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
        var fakeAxisFactoryMock = new Mock<IFakeAxisFactory>();
        var axisFactory = new AxisFactory(
            systemConfiguration,
            timeManager,
            fakeAxisFactoryMock.Object
        );

        var axis = axisFactory.Create();
        if (fakeMode)
            fakeAxisFactoryMock.Verify(m => m.Create(skipWait), Times.Once);
        else
            axis.Should().BeOfType(typeof(Axis));
    }
}
