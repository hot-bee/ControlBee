using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests;

[TestSubject(typeof(Class1))]
public class Class1Test
{
    [Fact]
    public void FooTest()
    {
        Assert.Equal("bar", new Class1().Foo());
    }
}
