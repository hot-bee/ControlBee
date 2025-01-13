﻿using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Array3D<>))]
public class Array3DTest
{
    [Fact]
    public void SerializeTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var array = new Array3D<int>(3, 3, 3);
        array[0, 1, 2] = 10;
        array[0, 1, 2].Should().Be(10);

        var expectedJson = """
            {
                "Size": [3,3,3],
                "Values": [0,0,0,0,0,10,0,0,0,
                0,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,0]
            }
            """;

        var expectedJToken = JToken.Parse(expectedJson);
        var actualJToken = JToken.Parse(JsonSerializer.Serialize(array));

        actualJToken.Should().BeEquivalentTo(expectedJToken);
    }

    [Fact]
    public void DeserializeTest()
    {
        var array = new Array3D<int>();
        const string json = """
            {
                "Size": [3,3,3],
                "Values": [0,0,0,0,0,10,0,0,0,
                0,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,0]
            }
            """;
        array.ReadJson(JsonDocument.Parse(json));
        Assert.Equal(10, array[0, 1, 2]);
    }

    [Fact]
    public void PartialValueChangedTest()
    {
        var array = new Array3D<int>(3, 3, 3);
        var called = false;
        array.ValueChanged += (sender, e) =>
        {
            e.Location.Should().Be((0, 1, 2));
            e.OldValue.Should().Be(0);
            e.NewValue.Should().Be(10);
            called = true;
        };
        array[0, 1, 2] = 10;
        called.Should().BeTrue();
    }

    [Fact]
    public void NewElementsTest()
    {
        var array = new Array3D<String>(1, 1, 1);
        array[0, 0, 0].Should().NotBeNull();
    }

    [Fact]
    public void UpdateSubItemTest()
    {
        var array = new Array3D<Position1D>(1, 1, 1);
        var actor = new Actor();
        array.Actor = actor;
        array.ItemName = "myItem";
        array.UpdateSubItem();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var itemSub = (IActorItemSub)array[0, 0, 0];
        itemSub.Actor.Should().Be(actor);
        itemSub.ItemName.Should().Be("myItem");
    }
}
