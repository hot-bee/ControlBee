using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AxisFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager,
    IScenarioFlowTester flowTester
) : IAxisFactory
{
    public IAxis Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeAxis(timeManager, flowTester, systemConfigurations.SkipWaitSensor)
            : new Axis(deviceManager, timeManager);
    }
}
