using ControlBee.Variables;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(String))]
public class StringTest
{
    [Fact]
    public void ValueChangedTest()
    {
        var stringData = new String("Hello");
        Assert.AreEqual("Hello", stringData.ToString());

        var called = false;
        stringData.ValueChanged += (s, e) =>
        {
            CollectionAssert.AreEqual(
                new object[] { "Value" },
                e.Location
            );
            Assert.AreEqual("Hello", e.OldValue);
            Assert.AreEqual("World", e.NewValue);
            called = true;
        };
        stringData.Value = "World";
        Assert.AreEqual("World", stringData.ToString());
        Assert.IsTrue(called);
    }
}
