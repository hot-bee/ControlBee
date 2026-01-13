using ControlBee.Variables;
using JetBrains.Annotations;
using Xunit;
using Assert = Xunit.Assert;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(String))]
public class StringTest
{
    [Fact]
    public void ValueChangedTest()
    {
        var stringData = new String("Hello");
        Assert.Equal("Hello", stringData.ToString());

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
