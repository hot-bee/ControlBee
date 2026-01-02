using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(VariableManager))]
public class VariableManagerTest : ActorFactoryBase
{
    [Fact]
    public void SaveTest()
    {
        var actor = ActorFactory.Create<Actor>("myActor");
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        VariableManager.LocalName.Should().Be("Default");
        VariableManager.Save("myRecipe");
        Mock.Get(Database)
            .Verify(
                m => m.WriteVariables(VariableScope.Local, "myRecipe", "myActor", "myId", "1"),
                Times.Once
            );
        VariableManager.LocalName.Should().Be("myRecipe");
    }

    [Fact]
    public void LoadTest()
    {
        Mock.Get(Database).Setup(m => m.Read("myRecipe", "MyActor", "myId")).Returns((10, "2"));
        VariableManager.LocalName.Should().Be("Default");
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        VariableManager.Load("myRecipe");
        variable.Value.Should().Be(2);
        VariableManager.LocalName.Should().Be("myRecipe");
    }

    [Fact]
    public void DuplicateUidTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var actor2 = ActorFactory.Create<Actor>("MyActor2");
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
        var systemConfigurations = new SystemConfigurations();
        var variableManager = new VariableManager(
            databaseMock.Object,
            EmptyActorRegistry.Instance,
            systemConfigurations,
            EmptyDeviceManager.Instance
        );
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
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);

        var actor = ActorFactory.Create<Actor>("myActor");
        var variable = new Variable<int>(actor, "/myVar", VariableScope.Global);

        variable.Value = 1;
        var match = new Func<Message, bool>(message =>
        {
            if (message.Name != "_itemDataChanged")
                return false;
            var actorItemMessage = (ActorItemMessage)message;
            var valueChangedArgs =
                actorItemMessage.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
            return actorItemMessage.ActorName == "myActor"
                && actorItemMessage.ItemPath == "/myVar"
                && valueChangedArgs?.Location == (object[])[]
                && valueChangedArgs.OldValue as int? == 0
                && valueChangedArgs.NewValue as int? == 1;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }
}
