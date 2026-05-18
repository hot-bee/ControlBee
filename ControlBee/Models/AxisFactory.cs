using ControlBee.Interfaces;
using ControlBee.Services;

namespace ControlBee.Models;

public class AxisFactory(
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager,
    IScenarioFlowTester flowTester,
    IDeviceMonitor deviceMonitor,
    IInitializeSequenceFactory initializeSequenceFactory
) : IAxisFactory
{
    public IAxis Create()
    {
        var axis = systemConfigurations.FakeMode
            ? new FakeAxis(
                systemConfigurations,
                deviceManager,
                timeManager,
                flowTester,
                systemConfigurations.SkipWaitSensor,
                initializeSequenceFactory
            )
            : new Axis(systemConfigurations, deviceManager, timeManager, initializeSequenceFactory);
        deviceMonitor.Add(axis);
        return axis;
    }
}
