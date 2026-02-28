using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(PositionAxesMap))]
public class PositionAxesMapTest : ActorFactoryBase
{
    [Fact]
    public void AddPositionAxisTest()
    {
        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;

        var axisYMock = new Mock<IAxis>();
        var axisY = axisYMock.Object;

        var actor = ActorFactory.Create<TestActor>("testActor", axisX, axisY);

        actor.Position.Value[0] = 10.0;
        actor.Position.Value[1] = 10.0;
        actor.Position.Value.Move();
        axisXMock.Verify(m => m.Move(It.IsAny<double>(), false), Times.Once);
        axisYMock.Verify(m => m.Move(It.IsAny<double>(), false), Times.Once);
    }

    [Fact]
    public void SkipMoveWhenAlreadyAtTargetPositionTest()
    {
        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;

        var axisYMock = new Mock<IAxis>();
        var axisY = axisYMock.Object;

        var actor = ActorFactory.Create<TestActor>("testActor", axisX, axisY);

        // Default mock GetPosition returns 0, which matches the default position (0, 0)
        actor.Position.Value.Move();
        axisXMock.Verify(m => m.Move(It.IsAny<double>(), It.IsAny<bool>()), Times.Never);
        axisYMock.Verify(m => m.Move(It.IsAny<double>(), It.IsAny<bool>()), Times.Never);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class TestActor : Actor
    {
        public readonly Variable<Position2D> Position = new();
        private readonly IAxis X;
        private readonly IAxis Y;

        public TestActor(ActorConfig config, IAxis x, IAxis y)
            : base(config)
        {
            X = x;
            Y = y;
            PositionAxesMap.Add(Position, [X, Y]);
        }
    }
}
