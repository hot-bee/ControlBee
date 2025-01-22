﻿using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;
using String = ControlBee.Variables.String;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Variable<>))]
public class VariableTest
{
    [Fact]
    public void IntVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var intVariable = new Variable<int>(actor, "myId", VariableScope.Global, 1);
        Assert.Equal(1, intVariable.Value);

        var called = false;
        intVariable.ValueChanged += (s, e) =>
        {
            Assert.Null(e.Location);
            Assert.Equal(1, e.OldValue);
            Assert.Equal(2, e.NewValue);
            called = true;
        };
        intVariable.Value = 2;
        Assert.Equal(2, intVariable.Value);
        Assert.True(called);
    }

    [Fact]
    public void SerializeTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var intVariable = new Variable<int>(actor, "myId", VariableScope.Global, 1);
        Assert.Equal("1", intVariable.ToJson());

        var called = false;
        intVariable.ValueChanged += (s, e) =>
        {
            Assert.Null(e.Location);
            Assert.Equal(1, e.OldValue);
            Assert.Equal(2, e.NewValue);
            called = true;
        };
        intVariable.FromJson("2");
        Assert.Equal(2, intVariable.Value);
        Assert.True(called);
    }

    [Fact]
    public void StringVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var stringVariable = new Variable<String>(
            actor,
            "myId",
            VariableScope.Global,
            new String("Hello")
        );
        stringVariable.Value.ToString().Should().Be("Hello");

        var called = false;
        stringVariable.ValueChanged += (s, e) =>
        {
            e.Location.Should().BeNull();
            e.OldValue.Should().BeOfType<String>();
            e.NewValue.Should().BeOfType<String>();
            called = true;
        };
        stringVariable.Value = new String("World");
        Assert.Equal("World", stringVariable.Value.ToString());
        Assert.True(called);
    }

    [Fact]
    public void Array1DVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var arrayVariable = new Variable<Array1D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array1D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            e.Location.Should().BeNull();
            e.OldValue.Should().BeOfType<Array1D<int>>();
            e.NewValue.Should().BeOfType<Array1D<int>>();
            called = true;
        };
        arrayVariable.Value = new Array1D<int>(10);
        arrayVariable.Value.Size.Should().Be(10);
        Assert.True(called);
    }

    [Fact]
    public void Array2DVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var arrayVariable = new Variable<Array2D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array2D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            e.Location.Should().BeNull();
            e.OldValue.Should().BeOfType<Array2D<int>>();
            e.NewValue.Should().BeOfType<Array2D<int>>();
            called = true;
        };
        arrayVariable.Value = new Array2D<int>(10, 20);
        arrayVariable.Value.Size.Should().BeEquivalentTo((10, 20));
        Assert.True(called);
    }

    [Fact]
    public void Array3DVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                new EmptyAxisFactory(),
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManagerMock.Object,
                new TimeManager()
            )
        );
        var arrayVariable = new Variable<Array3D<int>>(
            actor,
            "myId",
            VariableScope.Global,
            new Array3D<int>()
        );

        var called = false;
        arrayVariable.ValueChanged += (s, e) =>
        {
            e.Location.Should().BeNull();
            e.OldValue.Should().BeOfType<Array3D<int>>();
            e.NewValue.Should().BeOfType<Array3D<int>>();
            called = true;
        };
        arrayVariable.Value = new Array3D<int>(10, 20, 30);
        arrayVariable.Value.Size.Should().BeEquivalentTo((10, 20, 30));
        Assert.True(called);
    }

    [Fact]
    public void ProcessMessageTest()
    {
        var database = Mock.Of<IDatabase>();
        var variableManager = new VariableManager(database);
        var actor = new Actor("myActor");
        var intVariable = new Variable<int>(
            variableManager,
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
            var payload = (ValueChangedEventArgs)actorItemMessage.Payload;
            if (payload == null)
                return false;
            return actorItemMessage.Name == "_itemData"
                && payload.Location == null
                && payload.OldValue == null
                && (int)payload.NewValue! == 1
                && actorItemMessage.ActorName == "myActor"
                && actorItemMessage.ItemPath == "/myVar";
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var database = Mock.Of<IDatabase>();
        var variableManager = new VariableManager(database);
        var actor = new Actor("myActor");
        var intVariable = new Variable<int>(
            variableManager,
            actor,
            "/myVar",
            VariableScope.Global,
            1
        );
        var uiActor = Mock.Of<IActor>();
        var reqMessage = new ActorItemMessage(uiActor, "/myVar", "_itemDataWrite", 2);

        intVariable.ProcessMessage(reqMessage);
        Assert.Equal(2, intVariable.Value);
    }
}
