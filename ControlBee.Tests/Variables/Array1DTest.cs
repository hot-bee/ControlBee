using System;
using System.Text.Json;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Xunit;
using String = ControlBee.Variables.String;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Array1D<>))]
public class Array1DTest
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
            e.Location.Should().Be(1);
            e.OldValue.Should().Be(0);
            e.NewValue.Should().Be(10);
            called = true;
        };
        array[1] = 10;
        called.Should().BeTrue();
    }

    [Fact]
    public void StringElementTest()
    {
        var act = () => new Array1D<String>();
        act.Should().Throw<ApplicationException>();
    }
}
