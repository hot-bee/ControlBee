using System;
using System.Data;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(VariableManager))]
public class VariableManagerTest : ActorFactoryBase
{
    [Fact]
    public void SaveTest()
    {
        var actor = ActorFactory.Create<Actor>("myActor");
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        Assert.Equal("Default", VariableManager.LocalName);
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
        Assert.Equal("myRecipe", VariableManager.LocalName);
    }

    [Fact]
    public void LoadTest()
    {
        Mock.Get(Database).Setup(m => m.Read("myRecipe", "MyActor", "myId")).Returns((10, "2"));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());
        Assert.Equal("Default", VariableManager.LocalName);
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        VariableManager.Load("myRecipe");
        Assert.Equal(2, variable.Value);
        Assert.Equal("myRecipe", VariableManager.LocalName);
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
        Assert.Equal(1, variableManager.Count);

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

    [Fact]
    public void OverwriteOnParseFailTest()
    {
        Mock.Get(Database)
            .Setup(m => m.Read("myRecipe", "MyActor", "myVariable"))
            .Returns((10, "true"));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());
        var actor = ActorFactory.Create<Actor>("MyActor");
        _ = new Variable<double>(actor, "myVariable", VariableScope.Local, 0.5);
        VariableManager.Load("myRecipe");
        const string jsonString = "{\r\n  \"Version\": 2,\r\n  \"Value\": 0.5\r\n}";
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "MyActor",
                        "myVariable",
                        jsonString
                    ),
                Times.Once
            );
    }

    [Fact]
    public void LoadFallbackFromGlobalToLocalScopeTest()
    {
        const string jsonString = "{\r\n  \"Version\": 2,\r\n  \"Value\": 42\r\n}";
        Mock.Get(Database).Setup(m => m.Read("", "MyActor", "myId")).Returns((10, jsonString));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());

        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 0);
        VariableManager.Load("myRecipe");

        Assert.Equal(42, variable.Value);
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "MyActor",
                        "myId",
                        It.IsAny<string>()
                    ),
                Times.Once
            );
    }

    [Fact]
    public void LoadFallbackFromLocalToGlobalScopeTest()
    {
        const string jsonString = "{\r\n  \"Version\": 2,\r\n  \"Value\": 99\r\n}";
        Mock.Get(Database)
            .Setup(m => m.Read("myRecipe", "MyActor", "myId"))
            .Returns((10, jsonString));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());

        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Global, 0);
        VariableManager.Load("myRecipe");

        Assert.Equal(99, variable.Value);
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Global,
                        "",
                        "MyActor",
                        "myId",
                        It.IsAny<string>()
                    ),
                Times.Once
            );
    }

    [Fact]
    public void LoadFallbackFromGlobalToLocalScopeWithArray1DTest()
    {
        var helper = new Variable<Array1D<double>>(VariableScope.Global, new Array1D<double>(3));
        helper.Value[0] = 1.0;
        helper.Value[1] = 2.0;
        helper.Value[2] = 3.0;
        var jsonString = helper.ToJson();

        Mock.Get(Database).Setup(m => m.Read("", "MyActor", "myId")).Returns((10, jsonString));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());

        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<Array1D<double>>(
            actor,
            "myId",
            VariableScope.Local,
            new Array1D<double>(3)
        );
        VariableManager.Load("myRecipe");

        Assert.Equal(1.0, variable.Value[0]);
        Assert.Equal(2.0, variable.Value[1]);
        Assert.Equal(3.0, variable.Value[2]);
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "MyActor",
                        "myId",
                        It.IsAny<string>()
                    ),
                Times.Once
            );
    }

    [Fact]
    public void SaveWrapsWritesInTransactionTest()
    {
        var txMock = new Mock<IDatabaseTransaction>();
        Mock.Get(Database).Setup(m => m.BeginTransaction()).Returns(txMock.Object);

        var actor = ActorFactory.Create<Actor>("myActor");
        _ = new Variable<int>(actor, "myId", VariableScope.Local, 1);

        VariableManager.Save("myRecipe");

        Mock.Get(Database).Verify(m => m.BeginTransaction(), Times.Once);
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "myActor",
                        "myId",
                        It.IsAny<string>()
                    ),
                Times.Once
            );
        txMock.Verify(m => m.Commit(), Times.Once);
        txMock.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public void SaveStillDisposesTransactionWhenChangeWriteThrowsTest()
    {
        SystemConfigurations.AutoVariableSave = false;
        var txMock = new Mock<IDatabaseTransaction>();
        Mock.Get(Database).Setup(m => m.BeginTransaction()).Returns(txMock.Object);
        Mock.Get(Database)
            .Setup(m => m.WriteVariableChange(It.IsAny<IVariable>(), It.IsAny<ValueChangedArgs>()))
            .Throws(new InvalidOperationException("boom"));

        var actor = ActorFactory.Create<Actor>("myActor");
        var variable = new Variable<int>(actor, "myId", VariableScope.Local, 1);
        variable.Value = 2;

        Assert.Throws<InvalidOperationException>(() => VariableManager.Save());
        txMock.Verify(m => m.Commit(), Times.Never);
        txMock.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public void LoadFallbackFromGlobalToLocalScopeWithArray2DTest()
    {
        var helper = new Variable<Array2D<double>>(VariableScope.Global, new Array2D<double>(2, 2));
        helper.Value[0, 0] = 1.0;
        helper.Value[0, 1] = 2.0;
        helper.Value[1, 0] = 3.0;
        helper.Value[1, 1] = 4.0;
        var jsonString = helper.ToJson();

        Mock.Get(Database).Setup(m => m.Read("", "MyActor", "myId")).Returns((10, jsonString));
        Mock.Get(Database).Setup(m => m.ReadLatestVariableChanges()).Returns(new DataTable());

        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<Array2D<double>>(
            actor,
            "myId",
            VariableScope.Local,
            new Array2D<double>(2, 2)
        );
        VariableManager.Load("myRecipe");

        Assert.Equal(1.0, variable.Value[0, 0]);
        Assert.Equal(2.0, variable.Value[0, 1]);
        Assert.Equal(3.0, variable.Value[1, 0]);
        Assert.Equal(4.0, variable.Value[1, 1]);
        Mock.Get(Database)
            .Verify(
                m =>
                    m.WriteVariables(
                        VariableScope.Local,
                        "myRecipe",
                        "MyActor",
                        "myId",
                        It.IsAny<string>()
                    ),
                Times.Once
            );
    }
}
