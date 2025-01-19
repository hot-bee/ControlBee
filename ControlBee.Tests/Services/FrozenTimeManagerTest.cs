using System.Threading.Tasks;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(FrozenTimeManager))]
public class FrozenTimeManagerTest
{
    [Fact]
    public async Task TaskRunTest()
    {
        using var frozenTimeManager = new FrozenTimeManager();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);

        var task = frozenTimeManager.TaskRun(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.Equal(1, frozenTimeManager.RegisteredThreadsCount);
            fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
            fakeAxis.MoveAndWait(10.0);
        });
        await task;
        Assert.Equal(0, frozenTimeManager.RegisteredThreadsCount);
        Assert.Equal(10.0, fakeAxis.GetPosition(PositionType.Command));
    }
}
