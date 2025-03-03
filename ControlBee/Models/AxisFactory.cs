using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AxisFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager,
    IScenarioFlowTester flowTester,
    IDeviceMonitor deviceMonitor
) : IAxisFactory
{
    public IAxis Create()
    {
        var axis = systemConfigurations.FakeMode
            ? new FakeAxis(
                deviceManager,
                timeManager,
                flowTester,
                systemConfigurations.SkipWaitSensor
            )
            : new Axis(deviceManager, timeManager);
        deviceMonitor.Add(axis);
        return axis;
    }
}
