using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Moq;
using Xunit;
using String = ControlBee.Variables.String;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Variable<>))]
public class VariableTest : ActorFactoryBase
{
    [Fact]
    public void IntVariableTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var intVariable = new Variable<int>(actor, "myId", VariableScope.Global, 1);
        Assert.AreEqual(1, intVariable.Value);

        var called = false;
        intVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.AreEqual(1, e.OldValue);
            Assert.AreEqual(2, e.NewValue);
            called = true;
        };
        intVariable.Value = 2;
        Assert.AreEqual(2, intVariable.Value);
        Assert.IsTrue(called);
    }

    [Fact]
    public void SerializeTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var intVariable = new Variable<int>(actor, "myId", VariableScope.Global, 1);
        Assert.AreEqual("{\r\n  \"Version\": 2,\r\n  \"Value\": 1\r\n}", intVariable.ToJson());

        var called = false;
        intVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.AreEqual(1, e.OldValue);
            Assert.AreEqual(2, e.NewValue);
            called = true;
        };
        intVariable.FromJson("{\r\n  \"Version\": 2,\r\n  \"Value\": 2\r\n}");
        Assert.AreEqual(2, intVariable.Value);
        Assert.IsTrue(called);
    }

    [Fact]
    public void StringVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = ActorFactory.Create<Actor>("MyActor");
        var stringVariable = new Variable<String>(
            actor,
            "myId",
            VariableScope.Global,
            new String("Hello")
        );
        Assert.AreEqual("Hello", stringVariable.Value.ToString());

        var called = false;
        stringVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.IsInstanceOfType<String>(e.OldValue);
            Assert.IsInstanceOfType<String>(e.NewValue);
            called = true;
        };
        stringVariable.Value = new String("World");
        Assert.AreEqual("World", stringVariable.Value.ToString());
        Assert.IsTrue(called);
    }

    [Fact]
    public void Array1DVariableTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var arrayVariable = new Variable<Array1D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array1D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.IsInstanceOfType<Array1D<int>>(e.OldValue);
            Assert.IsInstanceOfType<Array1D<int>>(e.NewValue);
            called = true;
        };
        arrayVariable.Value = new Array1D<int>(10);
        Assert.AreEqual(10, arrayVariable.Value.Size);
        Assert.IsTrue(called);
    }

    [Fact]
    public void Array2DVariableTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var arrayVariable = new Variable<Array2D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array2D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.IsInstanceOfType<Array2D<int>>(e.OldValue);
            Assert.IsInstanceOfType<Array2D<int>>(e.NewValue);
            called = true;
        };
        arrayVariable.Value = new Array2D<int>(10, 20);
        Assert.AreEqual(10, arrayVariable.Value.Size.Item1);
        Assert.AreEqual(20, arrayVariable.Value.Size.Item2);
        Assert.IsTrue(called);
    }

    [Fact]
    public void Array3DVariableTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var arrayVariable = new Variable<Array3D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array3D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            Assert.AreEqual([], e.Location);
            Assert.IsInstanceOfType<Array3D<int>>(e.OldValue);
            Assert.IsInstanceOfType<Array3D<int>>(e.NewValue);
            called = true;
        };
        arrayVariable.Value = new Array3D<int>(10, 20, 30);
        Assert.AreEqual(10, arrayVariable.Value.Size.Item1);
        Assert.AreEqual(20, arrayVariable.Value.Size.Item2);
        Assert.AreEqual(30, arrayVariable.Value.Size.Item3);
        Assert.IsTrue(called);
    }

    [Fact]
    public void DataReadTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var intVariable = new Variable<int>(
            VariableManager,
            actor,
            "/myVar",
            VariableScope.Global,
            1
        );
        var uiActor = Mock.Of<IActor>();
        var reqMessage = new ActorItemMessage(uiActor, "/myVar", "_itemDataRead");

        intVariable.ProcessMessage(reqMessage);
        var match = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var valueChangedArgs =
                actorItemMessage.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
            return actorItemMessage.Name == "_itemDataChanged"
                && valueChangedArgs?.Location == (object[])[]
                && valueChangedArgs is { OldValue: 0, NewValue: 1 }
                && actorItemMessage is { ActorName: "MyActor", ItemPath: "/myVar" };
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void MetaDataReadTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var intVariable = new Variable<int>(
            VariableManager,
            actor,
            "/MyVar",
            VariableScope.Global,
            1
        );
        var uiActor = Mock.Of<IActor>();
        var reqMessage = new ActorItemMessage(uiActor, "/MyVar", "_itemMetaDataRead");

        var actorItemInjectionDataSource = Mock.Of<ISystemPropertiesDataSource>();
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Name"))
            .Returns("My variable");
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Unit"))
            .Returns("bool");
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Desc"))
            .Returns("This is a my variable.");
        intVariable.InjectProperties(actorItemInjectionDataSource);
        intVariable.ProcessMessage(reqMessage);

        var match = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemMetaData", ActorName: "MyActor", ItemPath: "/MyVar" }
                && actorItemMessage.DictPayload!["Name"] as string == "My variable"
                && actorItemMessage.DictPayload!["Unit"] as string == "bool"
                && actorItemMessage.DictPayload!["Desc"] as string == "This is a my variable.";
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var intVariable = new Variable<int>(
            VariableManager,
            actor,
            "/myVar",
            VariableScope.Global,
            1
        );
        var uiActor = Mock.Of<IActor>();
        var reqMessage = new ActorItemMessage(
            uiActor,
            "/myVar",
            "_itemDataWrite",
            new ItemDataWriteArgs([], 2)
        );

        intVariable.ProcessMessage(reqMessage);
        Assert.AreEqual(2, intVariable.Value);
    }

    [Fact]
    public void InjectPropertiesTest()
    {
        SystemPropertiesDataSource.ReadFromString(
            @"
MyActor:
  MyVar1:
    Name: My Variable 1
    Unit: mm
    Desc: The first variable.
  MyVar2:
    Name: My Variable 2
    Desc: The second variable.
"
        );
        var actor = ActorFactory.Create<Actor>("MyActor");
        var myVariable1 = new Variable<int>(
            VariableManager,
            actor,
            "/MyVar1",
            VariableScope.Global,
            1
        );
        var myVariable2 = new Variable<int>(
            VariableManager,
            actor,
            "/MyVar2",
            VariableScope.Global,
            1
        );

        Assert.AreEqual("/MyVar1", myVariable1.Name);
        myVariable1.InjectProperties(SystemPropertiesDataSource);
        myVariable2.InjectProperties(SystemPropertiesDataSource);

        Assert.AreEqual("My Variable 1", myVariable1.Name);
        Assert.AreEqual("mm", myVariable1.Unit);
        Assert.AreEqual("The first variable.", myVariable1.Desc);
        Assert.AreEqual("My Variable 2", myVariable2.Name);
        Assert.AreEqual(string.Empty, myVariable2.Unit);
    }
}
