using ControlBee.Interfaces;
using ControlBee.Services;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(ActorRegistry))]
public class ActorRegistryTest
{
    [Fact]
    public void AddActorTest()
    {
        var actorRegistry = new ActorRegistry();
        var actor1 = Mock.Of<IActor>();
        var actor2 = Mock.Of<IActor>();
        actorRegistry.Add("actor1", actor1);
        actorRegistry.Get("actor1").Should().Be(actor1);

        actorRegistry.Add("actor2", actor2);
        actorRegistry.GetActorNames().Should().BeEquivalentTo("actor1", "actor2");
    }
}
