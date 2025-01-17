using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(ActorFactory))]
public class ActorFactoryTest
{
    [Fact]
    public void InitVariablesTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actorFactory = new ActorFactory(
            new EmptyAxisFactory(),
            variableManagerMock.Object,
            new TimeManager()
        );
        var actor = actorFactory.Create<ActorWithVariables>("testActor");
        variableManagerMock.Verify(m => m.Add(actor.Foo), Times.Once);
        variableManagerMock.Verify(m => m.Add(actor.Bar), Times.Once);
        variableManagerMock.Verify(m => m.Add(actor.PickupPosition), Times.Once);

        actor.PickupPosition.ActorName.Should().Be("testActor");
        actor.PickupPosition.ItemName.Should().Be("/PickupPosition");
        actor.PickupPosition.Value.Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value.ItemName.Should().Be("/PickupPosition");
        actor.PickupPosition.Value[0, 0].Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value[0, 0].ItemName.Should().Be("/PickupPosition");

        actor.PickupPosition.Value = new Array2D<Position1D>(10, 10);
        actor.PickupPosition.Value[0, 0].Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value[0, 0].ItemName.Should().Be("/PickupPosition");
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ActorWithVariables(ActorConfig config) : Actor(config)
    {
        public readonly Variable<double> Bar = new(VariableScope.Local);
        public readonly Variable<int> Foo = new(VariableScope.Global);
        public readonly Variable<Array2D<Position1D>> PickupPosition = new(
            VariableScope.Local,
            new Array2D<Position1D>(1, 1)
        );
    }
}
