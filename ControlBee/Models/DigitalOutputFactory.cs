using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalOutputFactory(
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager,
    IDeviceMonitor deviceMonitor
) : IDigitalOutputFactory
{
    public IDigitalOutput Create()
    {
        var output = systemConfigurations.FakeMode
            ? new FakeDigitalOutput(timeManager)
            : new DigitalOutput(deviceManager, timeManager);
        deviceMonitor.Add(output);
        return output;
    }
}
