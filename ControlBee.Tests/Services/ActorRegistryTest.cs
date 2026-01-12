using ControlBee.Interfaces;
using ControlBee.Services;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
        Assert.AreSame(actor1, actorRegistry.Get("actor1"));

        actorRegistry.Add(actor2);
        CollectionAssert.AreEquivalent(
            new[] { "actor1", "actor2" },
            actorRegistry.GetActorNames().ToList()
        );
    }
}
