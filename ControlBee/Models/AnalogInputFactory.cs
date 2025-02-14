using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AnalogInputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IDeviceMonitor deviceMonitor
) : IAnalogInputFactory
{
    public IAnalogInput Create()
    {
        var input = systemConfigurations.FakeMode
            ? new FakeAnalogInput()
            : new AnalogInput(deviceManager);
        deviceMonitor.Add(input);
        return input;
    }
}
