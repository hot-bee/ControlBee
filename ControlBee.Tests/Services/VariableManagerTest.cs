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
        var variableManager = new VariableManager(databaseMock.Object, EmptyActorRegistry.Instance);
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalOutputFactory.Instance,
                variableManager,
                new TimeManager()
            )
        );
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
        var variableManager = new VariableManager(databaseMock.Object, EmptyActorRegistry.Instance);
        databaseMock.Setup(m => m.Read("myRecipe", "myActor", "myId")).Returns("2");
        variableManager.LocalName.Should().Be("Default");
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalOutputFactory.Instance,
                variableManager,
                new TimeManager()
            )
        );
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        variableManager.Load("myRecipe");
        variable.Value.Should().Be(2);
        variableManager.LocalName.Should().Be("myRecipe");
    }

    [Fact]
    public void DuplicateUidTest()
    {
        var databaseMock = new Mock<IDatabase>();
        var variableManager = new VariableManager(databaseMock.Object, EmptyActorRegistry.Instance);
        var timeManager = new TimeManager();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalOutputFactory.Instance,
                variableManager,
                timeManager
            )
        );
        var actor2 = new Actor(
            new ActorConfig(
                "myActor2",
                new EmptyAxisFactory(),
                EmptyDigitalOutputFactory.Instance,
                variableManager,
                timeManager
            )
        );
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
        var variableManager = new VariableManager(databaseMock.Object, EmptyActorRegistry.Instance);
        var actor = new Mock<IActorInternal>();
        actor.Setup(m => m.Name).Returns("myActor");
        actor.Setup(m => m.VariableManager).Returns(variableManager);
        _ = new Variable<int>(actor.Object, "myId", VariableScope.Local);
        variableManager.Count.Should().Be(1);

        var act1 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        act1.Should().Throw<ApplicationException>();

        actor.Setup(m => m.Name).Returns("myActor2");
        var act2 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        act2.Should().NotThrow();
    }

    [Fact]
    public void VariableChangedTest()
    {
        var database = Mock.Of<IDatabase>();
        var actorRegistry = Mock.Of<IActorRegistry>();
        var variableManager = new VariableManager(database, actorRegistry);
        var actorFactory = new ActorFactory(
            new EmptyAxisFactory(),
            EmptyDigitalOutputFactory.Instance,
            variableManager,
            new EmptyTimeManager(),
            actorRegistry
        );

        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(actorRegistry).Setup(m => m.Get("ui")).Returns(uiActor);

        var actor = actorFactory.Create<Actor>("myActor");
        var variable = new Variable<int>(actor, "/myVar", VariableScope.Global);

        variable.Value = 1;
        var match = new Func<Message, bool>(message =>
        {
            if (message.Name != "_itemDataChanged")
                return false;
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (ValueChangedEventArgs)message.Payload!;
            return actorItemMessage.ActorName == "myActor"
                && actorItemMessage.ItemPath == "/myVar"
                && payload.Location == null
                && (int)payload.OldValue! == 0
                && (int)payload.NewValue! == 1;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }
}
