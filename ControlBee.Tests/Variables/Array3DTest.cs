using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Array3D<>))]
public class Array3DTest : ActorFactoryBase
{
    [Fact]
    public void SerializeTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var array = new Array3D<int>(3, 3, 3);
        array[0, 1, 2] = 10;
        Assert.AreEqual(10, array[0, 1, 2]);

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

        Assert.IsTrue(JToken.DeepEquals(actualJToken, expectedJToken));
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
        Assert.AreEqual(10, array[0, 1, 2]);
    }

    [Fact]
    public void PartialValueChangedTest()
    {
        var array = new Array3D<int>(3, 3, 3);
        var called = false;
        array.ValueChanged += (sender, e) =>
        {
            CollectionAssert.AreEqual(
                new object[] { (0, 1, 2) },
                e.Location
            );
            Assert.AreEqual(0, e.OldValue);
            Assert.AreEqual(10, e.NewValue);
            called = true;
        };
        array[0, 1, 2] = 10;
        Assert.IsTrue(called);
    }

    [Fact]
    public void NewElementsTest()
    {
        var array = new Array3D<String>(1, 1, 1);
        Assert.IsNotNull(array[0, 0, 0]);
    }

    [Fact]
    public void UpdateSubItemTest()
    {
        var array = new Array3D<Position1D>(1, 1, 1);
        var actor = ActorFactory.Create<Actor>("MyActor");
        array.Actor = actor;
        array.ItemPath = "myItem";
        array.UpdateSubItem();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var itemSub = (IActorItemSub)array[0, 0, 0];
        Assert.AreSame(actor, itemSub.Actor);
        Assert.AreEqual("myItem", itemSub.ItemPath);
    }
}
