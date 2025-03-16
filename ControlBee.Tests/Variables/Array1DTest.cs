﻿using System.Linq;
using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Array1D<>))]
public class Array1DTest : ActorFactoryBase
{
    [Fact]
    public void SerializeTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var array = new Array1D<int>(3);
        array[1] = 10;
        array[1].Should().Be(10);

        var expectedJson = """
            {
                "Size": [3],
                "Values": [0,10,0]
            }
            """;

        var expectedJToken = JToken.Parse(expectedJson);
        var actualJToken = JToken.Parse(JsonSerializer.Serialize(array));

        actualJToken.Should().BeEquivalentTo(expectedJToken);
    }

    [Fact]
    public void DeserializeTest()
    {
        var array = new Array1D<int>();
        const string json = """
            {
                "Size": [3],
                "Values": [0,10,0]
            }
            """;
        array.ReadJson(JsonDocument.Parse(json));
        array[1].Should().Be(10);
    }

    [Fact]
    public void PartialValueChangedTest()
    {
        var array = new Array1D<int>(3);
        var called = false;
        array.ValueChanged += (sender, e) =>
        {
            Assert.Equal([1], e.Location);
            Assert.Equal(0, e.OldValue);
            Assert.Equal(10, e.NewValue);
            called = true;
        };
        array[1] = 10;
        called.Should().BeTrue();
    }

    [Fact]
    public void NewElementsTest()
    {
        var array = new Array1D<String>(1);
        array[0].Should().NotBeNull();
    }

    [Fact]
    public void UpdateSubItemTest()
    {
        var array = new Array1D<Position1D>(1);
        var actor = ActorFactory.Create<Actor>("MyActor");
        array.Actor = actor;
        array.ItemPath = "myItem";
        array.UpdateSubItem();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var itemSub = (IActorItemSub)array[0];
        itemSub.Actor.Should().Be(actor);
        itemSub.ItemPath.Should().Be("myItem");
    }

    [Fact]
    public void ItemDataWriteTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (int)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual([0]));
                Assert.Equal(10, newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/MyVariable",
                "_itemDataWrite",
                new ItemDataWriteArgs([0], 10)
            )
        );

        actor.Start();
        actor.Join();
    }

    private class TestActor : Actor
    {
        public Variable<Array1D<int>> MyVariable = new(
            VariableScope.Temporary,
            new Array1D<int>(10)
        );

        public TestActor(ActorConfig config)
            : base(config) { }
    }
}
