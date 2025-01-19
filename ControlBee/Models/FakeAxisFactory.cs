using ControlBee.Interfaces;

namespace ControlBee.Models;

public class FakeAxisFactory(ITimeManager timeManager, IScenarioFlowTester flowTester)
    : IFakeAxisFactory
{
    public FakeAxis Create(bool skipWaitSensor)
    {
        return new FakeAxis(timeManager, flowTester, skipWaitSensor);
    }
}
