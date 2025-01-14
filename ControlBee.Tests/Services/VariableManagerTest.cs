using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(VariableManager))]
public class VariableManagerTest
{
    [Fact]
    public void SaveTest()
    {
        var databaseMock = new Mock<IDatabase>();
        var variableManager = new VariableManager(databaseMock.Object);
        var actor = new Actor(new ActorConfig("myActor", variableManager, new TimeManager()));
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        variableManager.LocalName.Should().Be("Default");
        variableManager.Save("myRecipe");
        databaseMock.Verify(
            m => m.Write(VariableScope.Local, "myRecipe", "myActor", "myId", "1"),
            Times.Once
        );
        variableManager.LocalName.Should().Be("myRecipe");
    }

    [Fact]
    public void LoadTest()
    {
        var databaseMock = new Mock<IDatabase>();
        var variableManager = new VariableManager(databaseMock.Object);
        databaseMock.Setup(m => m.Read("myRecipe", "myActor", "myId")).Returns("2");
        variableManager.LocalName.Should().Be("Default");
        var actor = new Actor(new ActorConfig("myActor", variableManager, new TimeManager()));
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        variableManager.Load("myRecipe");
        variable.Value.Should().Be(2);
        variableManager.LocalName.Should().Be("myRecipe");
    }

    [Fact]
    public void DuplicateUidTest()
    {
        var databaseMock = new Mock<IDatabase>();
        var variableManager = new VariableManager(databaseMock.Object);
        var timeManager = new TimeManager();
        var actor = new Actor(new ActorConfig("myActor", variableManager, timeManager));
        var actor2 = new Actor(new ActorConfig("myActor2", variableManager, timeManager));
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);

        var act1 = () => new Variable<int>(actor, "myId", VariableScope.Local, 1);
        act1.Should().Throw<ApplicationException>();

        var act2 = () => new Variable<int>(actor, "myId", VariableScope.Global, 1);
        act2.Should().Throw<ApplicationException>();

        var act3 = () => new Variable<int>(actor2, "myId", VariableScope.Local, 1);
        act3.Should().NotThrow();

        var act4 = () => new Variable<int>(actor, "myId2", VariableScope.Local, 1);
        act4.Should().NotThrow();
    }

    [Fact]
    public void VariableInActorTest()
    {
        var databaseMock = new Mock<IDatabase>();
        var variableManager = new VariableManager(databaseMock.Object);
        var actor = new Mock<IActorInternal>();
        actor.Setup(m => m.ActorName).Returns("myActor");
        actor.Setup(m => m.VariableManager).Returns(variableManager);
        _ = new Variable<int>(actor.Object, "myId", VariableScope.Local);
        variableManager.Count.Should().Be(1);

        var act1 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        act1.Should().Throw<ApplicationException>();

        actor.Setup(m => m.ActorName).Returns("myActor2");
        var act2 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        act2.Should().NotThrow();
    }
}
