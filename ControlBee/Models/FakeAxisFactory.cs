using ControlBee.Interfaces;

namespace ControlBee.Models;

public class FakeAxisFactory(IFrozenTimeManager timeManager, IScenarioFlowTester flowTester)
    : IFakeAxisFactory
{
    public FakeAxis Create(bool emulationMode)
    {
        return new FakeAxis(timeManager, flowTester, emulationMode);
    }
}
