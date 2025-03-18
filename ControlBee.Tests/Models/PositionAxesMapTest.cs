using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeTest.Utils;
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

        actor.Position.Value.Move();
        axisXMock.Verify(m => m.Move(It.IsAny<double>(), false), Times.Once);
        axisYMock.Verify(m => m.Move(It.IsAny<double>(), false), Times.Once);
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
