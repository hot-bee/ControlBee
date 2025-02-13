using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AnalogInputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager
) : IAnalogInputFactory
{
    public IAnalogInput Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeAnalogInput()
            : new AnalogInput(deviceManager);
    }
}
