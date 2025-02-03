using ControlBee.Utils;
using JetBrains.Annotations;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Utils;

[TestSubject(typeof(DictCopy))]
public class DictCopyTest
{
    [Fact]
    public void CopyTest()
    {
        var myDict = new Dict
        {
            ["A"] = 1,
            ["SecondDict"] = new Dict
            {
                ["B"] = 2,
                ["ThirdDict"] = new Dict { ["C"] = 3 },
            },
        };
        var copied = DictCopy.Copy(myDict);
        copied["A"] = 0;
        (copied["SecondDict"] as Dict)!["B"] = 0;
        ((copied["SecondDict"] as Dict)!["ThirdDict"] as Dict)!["C"] = 0;

        Assert.Equal(1, myDict["A"]);
        Assert.Equal(2, (myDict["SecondDict"] as Dict)!["B"]);
        Assert.Equal(3, ((myDict["SecondDict"] as Dict)!["ThirdDict"] as Dict)!["C"]);
    }
}
