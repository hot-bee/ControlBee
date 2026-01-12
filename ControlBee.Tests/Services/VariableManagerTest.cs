using System;
using System.Data;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
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
        Assert.AreEqual("Default", VariableManager.LocalName);
        VariableManager.Save("myRecipe");
        const string jsonString = "{\r\n  \"Version\": 2,\r\n  \"Value\": 1\r\n}";
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "myActor",
                        "myId",
                        jsonString
                    ),
                Times.Once
            );
        Assert.AreEqual("myRecipe", VariableManager.LocalName);
    }

    [Fact]
    public void LoadTest()
    {
        Mock.Get(Database).Setup(m => m.Read("myRecipe", "MyActor", "myId")).Returns((10, "2"));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());
        Assert.AreEqual("Default", VariableManager.LocalName);
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        VariableManager.Load("myRecipe");
        Assert.AreEqual(2, variable.Value);
        Assert.AreEqual("myRecipe", VariableManager.LocalName);
    }

    [Fact]
    public void DuplicateUidTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var actor2 = ActorFactory.Create<Actor>("MyActor2");
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);

        var act1 = () => new Variable<int>(actor, "myId", VariableScope.Local, 1);
        Assert.Throws<ApplicationException>(() => act1());

        var act2 = () => new Variable<int>(actor, "myId", VariableScope.Global, 1);
        Assert.Throws<ApplicationException>(() => act2());

        var act3 = () => new Variable<int>(actor2, "myId", VariableScope.Local, 1);
        try
        {
            act3();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception was thrown: {ex}");
        }

        var act4 = () => new Variable<int>(actor, "myId2", VariableScope.Local, 1);
        try
        {
            act4();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception was thrown: {ex}");
        }
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
        Assert.AreEqual(1, variableManager.Count);

        var act1 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        Assert.Throws<ApplicationException>(() => act1());

        actor.Setup(m => m.Name).Returns("myActor2");
        var act2 = () => new Variable<int>(actor.Object, "myId", VariableScope.Local);
        try
        {
            act2();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception was thrown: {ex}");
        }
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
