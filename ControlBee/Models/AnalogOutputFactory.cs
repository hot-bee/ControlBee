using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AnalogOutputFactory(
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IDeviceMonitor deviceMonitor
) : IAnalogOutputFactory
{
    public IAnalogOutput Create()
    {
        var output = systemConfigurations.FakeMode
            ? new FakeAnalogOutput()
            : new AnalogOutput(deviceManager);
        deviceMonitor.Add(output);
        return output;
    }
}
