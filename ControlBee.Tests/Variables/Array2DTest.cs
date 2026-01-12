using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Array2D<>))]
public class Array2DTest : ActorFactoryBase
{
    [Fact]
    public void SerializeTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var array = new Array2D<int>(3, 3);
        array[1, 2] = 10;
        Assert.AreEqual(10, array[1, 2]);

        var expectedJson = """
            {
                "Size": [3,3],
                "Values": [0,0,0,0,0,10,0,0,0]
            }
            """;

        var expectedJToken = JToken.Parse(expectedJson);
        var actualJToken = JToken.Parse(JsonSerializer.Serialize(array));

        Assert.IsTrue(JToken.DeepEquals(actualJToken, expectedJToken));
    }

    [Fact]
    public void DeserializeTest()
    {
        var array = new Array2D<int>();
        const string json = """
            {
                "Size": [3,3],
                "Values": [0,0,0,0,0,10,0,0,0]
            }
            """;
        array.ReadJson(JsonDocument.Parse(json));

        Assert.AreEqual(10, array[1, 2]);
    }

    [Fact]
    public void PartialValueChangedTest()
    {
        var array = new Array2D<int>(3, 3);
        var called = false;
        array.ValueChanged += (sender, e) =>
        {
            CollectionAssert.AreEqual(
                new object[] { (1, 2) },
                e.Location
            );
            Assert.AreEqual(0, e.OldValue);
            Assert.AreEqual(10, e.NewValue);
            called = true;
        };
        array[1, 2] = 10;
        Assert.IsTrue(called);
    }

    [Fact]
    public void NewElementsTest()
    {
        var array = new Array2D<String>(1, 1);
        Assert.IsNotNull(array[0, 0]);
    }

    [Fact]
    public void UpdateSubItemTest()
    {
        var array = new Array2D<Position1D>(1, 1);
        var actor = ActorFactory.Create<Actor>("MyActor");
        array.Actor = actor;
        array.ItemPath = "myItem";
        array.UpdateSubItem();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var itemSub = (IActorItemSub)array[0, 0];
        Assert.AreSame(actor, itemSub.Actor);
        Assert.AreEqual("myItem", itemSub.ItemPath);
    }

    [Fact]
    public void CloneTest()
    {
        var array = new Array2D<int>(2, 2);
        var cloned = (Array2D<int>)array.Clone();
        array[0, 0] = 1;
        array[1, 1] = 2;
        cloned[0, 0] = 3;
        cloned[1, 1] = 4;
        Assert.AreEqual(1, array[0, 0]);
        Assert.AreEqual(2, array[1, 1]);
        Assert.AreEqual(3, cloned[0, 0]);
        Assert.AreEqual(4, cloned[1, 1]);
    }
}
