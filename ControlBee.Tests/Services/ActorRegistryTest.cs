using ControlBee.Interfaces;
using ControlBee.Models;
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
        Mock.Get(actor1).Setup(m => m.Name).Returns("actor1");
        var actor2 = Mock.Of<IActor>();
        Mock.Get(actor2).Setup(m => m.Name).Returns("actor2");
        actorRegistry.Add(actor1);
        actorRegistry.Get("actor1").Should().Be(actor1);

        actorRegistry.Add(actor2);
        actorRegistry.GetActorNames().Should().BeEquivalentTo("actor1", "actor2");
    }
}
