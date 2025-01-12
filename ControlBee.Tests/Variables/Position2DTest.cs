using System;
using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position2D))]
public class Position2DTest
{
    [Fact]
    public void VectorOperationTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        position.Vector += DenseVector.OfArray([1, 2]);
        position.Vector.Should().BeEquivalentTo(DenseVector.OfArray([2.2, 5.4]));
    }

    [Fact]
    public void AccessByIndexTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        position[0].Should().Be(1.2);
        position[1].Should().Be(3.4);
        position.Vector.Should().BeEquivalentTo(DenseVector.OfArray([1.2, 3.4]));

        position[0] = 10.1;
        position[1] = 11.2;
        position[0].Should().Be(10.1);
        position[1].Should().Be(11.2);
        position.Vector.Should().BeEquivalentTo(DenseVector.OfArray([10.1, 11.2]));
    }

    [Fact]
    public void ChangeVectorByIndexTest()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        position.Vector[0] = 10.1;

        var act1 = () => position.Vector[0];
        act1.Should().Throw<ApplicationException>();

        var act2 = () => position.Vector;
        act2.Should().Throw<ApplicationException>();

        var act3 = () => position.Vector = DenseVector.OfArray([0, 0]);
        act3.Should().Throw<ApplicationException>();
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
        actualToken.Should().BeEquivalentTo(expectedToken);
    }

    [Fact]
    public void DeserializeTest()
    {
        const string jsonString = """
            {"Values": [1.2, 3.4]}
            """;
        var position = JsonSerializer.Deserialize<Position2D>(jsonString);
        position.Values.Should().BeEquivalentTo([1.2, 3.4]);
    }

    [Fact]
    public void MoveWithoutAxesSettingTest()
    {
        var position = new Position2D(DenseVector.OfArray([1.2, 3.4]));
        var act = () => position.Move();
        act.Should().Throw<ApplicationException>();
    }

    [Fact]
    public void MoveWithAxesSettingTest()
    {
        var actor = new Actor();
        var variable = new Variable<Position2D>(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([1.2, 3.4]))
        );
        variable.Actor = actor;
        variable.GroupName = "myActor";
        variable.Uid = "homePosition";
        variable.UpdateSubItem();

        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;
        var axisYMock = new Mock<IAxis>();
        var axisY = axisYMock.Object;
        actor.PositionAxesMap.Add(variable, [axisX, axisY]);
        actor.PositionAxesMap.UpdateMap();

        var act = () => variable.Value.Move();
        act.Should().NotThrow();
        axisXMock.Verify(m => m.Move(1.2), Times.Once);
        axisYMock.Verify(m => m.Move(3.4), Times.Once);
    }

    [Fact]
    public void MoveWithWrongAxesSettingTest()
    {
        var actor = new Actor();
        var variable = new Variable<Position2D>(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([1.2, 3.4]))
        );
        variable.Actor = actor;
        variable.GroupName = "myActor";
        variable.Uid = "homePosition";
        variable.UpdateSubItem();

        var axisXMock = new Mock<IAxis>();
        var axisX = axisXMock.Object;
        actor.PositionAxesMap.Add(variable, [axisX]);
        actor.PositionAxesMap.UpdateMap();

        var act = () => variable.Value.Move();
        act.Should().Throw<ApplicationException>();
    }
}
