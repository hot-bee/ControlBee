using System.Collections.Generic;
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

    [Fact]
    public void CopyFromObjDict()
    {
        var myDict = new Dictionary<object, object>
        {
            ["A"] = 1,
            ["SecondDict"] = new Dictionary<object, object>
            {
                ["B"] = 2,
                ["ThirdDict"] = new Dictionary<object, object> { ["C"] = 3 },
            },
        };
        var copied = DictCopy.Copy(myDict);
        copied["A"] = 0;
        (copied["SecondDict"] as Dict)!["B"] = 0;
        ((copied["SecondDict"] as Dict)!["ThirdDict"] as Dict)!["C"] = 0;

        Assert.Equal(1, myDict["A"]);
        Assert.Equal(2, (myDict["SecondDict"] as Dictionary<object, object>)!["B"]);
        Assert.Equal(
            3,
            (
                (myDict["SecondDict"] as Dictionary<object, object>)!["ThirdDict"]
                as Dictionary<object, object>
            )!["C"]
        );
    }
}
