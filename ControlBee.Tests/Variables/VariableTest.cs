using ControlBee.Interfaces;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Variable<>))]
public class VariableTest
{
    [Fact]
    public void IntVariableTest()
    {
        var variableManagerMock = new Mock<IVariableManager>();
        var intVariable = new Variable<int>(
            variableManagerMock.Object,
            "myGroup",
            "myId",
            VariableScope.Global,
            1
        );
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
        var intVariable = new Variable<int>(
            variableManagerMock.Object,
            "myGroup",
            "myId",
            VariableScope.Global,
            1
        );
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
        var stringVariable = new Variable<String>(
            variableManagerMock.Object,
            "myGroup",
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
}
