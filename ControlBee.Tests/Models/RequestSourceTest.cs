using ControlBee.Models;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(RequestSource))]
public class RequestSourceTest
{
    [Fact]
    public void EqualsTest()
    {
        var source1 = new RequestSource(EmptyActor.Instance, "Hello");
        var source2 = new RequestSource(EmptyActor.Instance, "Hello");
        var source3 = new RequestSource(EmptyActor.Instance, "Hello!");
        Assert.Equal(source1, source2);
        Assert.NotEqual(source1, source3);
    }
}
