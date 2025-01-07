using System;
using System.Text.Json;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Xunit;
using String = ControlBee.Variables.String;

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
    public void StringElementTest()
    {
        var act = () => new Array3D<String>();
        act.Should().Throw<ApplicationException>();
    }
}
