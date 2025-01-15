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
    [InlineData(false)]
    [InlineData(true)]
    public void CreateTest(bool emulationMode)
    {
        var systemConfiguration = new SystemConfigurations();
        var frozenTimeManager = Mock.Of<IFrozenTimeManager>();
        var fakeAxisFactoryMock = new Mock<IFakeAxisFactory>();
        var axisFactory = new AxisFactory(
            systemConfiguration,
            frozenTimeManager,
            fakeAxisFactoryMock.Object
        );

        systemConfiguration.EmulationMode = emulationMode;
        var axis = axisFactory.Create();
        if (emulationMode)
            fakeAxisFactoryMock.Verify(m => m.Create(true), Times.Once);
        else
            axis.Should().BeOfType(typeof(Axis));
    }
}
