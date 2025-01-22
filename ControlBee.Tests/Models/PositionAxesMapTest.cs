using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(PositionAxesMap))]
public class PositionAxesMapTest
{
    [Fact]
    public void AddPositionAxisTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actorFactory = new ActorFactory(
            EmptyAxisFactory.Instance,
            EmptyDigitalInputFactory.Instance,
            EmptyDigitalOutputFactory.Instance,
            variableManagerMock.Object,
            new TimeManager(),
            Mock.Of<IActorRegistry>()
        );

        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;

        var axisYMock = new Mock<IAxis>();
        var axisY = axisYMock.Object;

        var actor = actorFactory.Create<TestActor>("testActor", axisX, axisY);

        actor.Position.Value.Move();
        axisXMock.Verify(m => m.Move(It.IsAny<double>()), Times.Once);
        axisYMock.Verify(m => m.Move(It.IsAny<double>()), Times.Once);
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
