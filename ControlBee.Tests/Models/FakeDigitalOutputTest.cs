using ControlBee.Models;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalOutput))]
public class FakeDigitalOutputTest
{
    [Fact]
    public void OnOffTest()
    {
        var fakeDigitalOutput = new FakeDigitalOutput();
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = true;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = false;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = true;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = false;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);
    }
}
