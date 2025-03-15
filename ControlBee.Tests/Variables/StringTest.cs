using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(String))]
public class StringTest
{
    [Fact]
    public void ValueChangedTest()
    {
        var stringData = new String("Hello");
        stringData.ToString().Should().Be("Hello");

        var called = false;
        stringData.ValueChanged += (s, e) =>
        {
            Assert.Equal(["Value"], e.Location);
            Assert.Equal("Hello", e.OldValue);
            Assert.Equal("World", e.NewValue);
            called = true;
        };
        stringData.Value = "World";
        Assert.Equal("World", stringData.ToString());
        Assert.True(called);
    }
}
