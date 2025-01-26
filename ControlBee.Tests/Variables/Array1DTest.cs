using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
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
            e.Location.Should().Be(1);
            e.OldValue.Should().Be(0);
            e.NewValue.Should().Be(10);
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
}
