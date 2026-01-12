using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position2D))]
public class Position2DTest : ActorFactoryBase
{
    [Fact]
    public void InitialValuesTest()
    {
        var position = new Position2D(DenseVector.OfArray([1, 2]));
        Assert.AreEqual(DenseVector.OfArray([1, 2]), position.Vector);
    }

    [Fact]
    public void VectorOperationTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        position.Vector += DenseVector.OfArray([1, 2]);
        var actual = position.Vector.ToArray();
        var expected = new[] { 2.2, 5.4 };
        CollectionAssert.AreEqual(expected, actual);
    }

    [Fact]
    public void AccessByIndexTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        Assert.AreEqual(1.2, position[0]);
        Assert.AreEqual(3.4, position[1]);
        CollectionAssert.AreEqual(
            new[] { 1.2, 3.4 },
            position.Vector.ToArray()
        );

        position[0] = 10.1;
        position[1] = 11.2;
        Assert.AreEqual(10.1, position[0]);
        Assert.AreEqual(11.2, position[1]);
        CollectionAssert.AreEqual(
            new[] { 10.1, 11.2 },
            position.Vector.ToArray()
        );
    }

    [Fact]
    public void ChangeVectorByIndexTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        position.Vector[0] = 10.1;

        var act1 = () => position.Vector[0];
        Assert.Throws<ApplicationException>(() => act1());

        var act2 = () => position.Vector;
        Assert.Throws<ApplicationException>(() => act2());

        var act3 = () => position.Vector = DenseVector.OfArray([0, 0]);
        Assert.Throws<ApplicationException>(() => act3());
    }

    [Fact]
    public void SerializeTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));

        const string expectedJson = """
            {"Values": [1.2, 3.4]}
            """;
        var expectedToken = JToken.Parse(expectedJson);
        var actualToken = JToken.Parse(JsonSerializer.Serialize(position));
        Assert.IsTrue(JToken.DeepEquals(actualToken, expectedToken));
    }

    [Fact]
    public void DeserializeTest()
    {
        const string jsonString = """
            {"Values": [1.2, 3.4]}
            """;
        var position = JsonSerializer.Deserialize<Position2D>(jsonString)!;
        CollectionAssert.AreEqual(
            new[] { 1.2, 3.4 },
            position.Values.ToArray()
        );
    }

    [Fact]
    public void MoveWithoutAxesSettingTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        var act = () => position.Move();
        Assert.Throws<ApplicationException>(() => act());
    }

    [Fact]
    public void MoveWithAxesSettingTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<Position2D>(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([1.2, 3.4]))
        );
        variable.Actor = actor;
        variable.ItemPath = "homePosition";
        variable.UpdateSubItem();

        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;
        var axisYMock = new Mock<IAxis>();
        var axisY = axisYMock.Object;
        actor.PositionAxesMap.Add(variable, [axisX, axisY]);
        actor.PositionAxesMap.UpdateMap();

        var act = () => variable.Value.Move();
        try
        {
            act();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception was thrown: {ex}");
        }
        axisXMock.Verify(m => m.Move(1.2, false), Times.Once);
        axisYMock.Verify(m => m.Move(3.4, false), Times.Once);
    }

    [Fact]
    public void MoveWithWrongAxesSettingTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = new Variable<Position2D>(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([1.2, 3.4]))
        );
        variable.Actor = actor;
        variable.ItemPath = "homePosition";
        variable.UpdateSubItem();

        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;
        actor.PositionAxesMap.Add(variable, [axisX]);
        actor.PositionAxesMap.UpdateMap();

        var act = () => variable.Value.Move();
        Assert.Throws<ApplicationException>(() => act());
    }
}
