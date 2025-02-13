using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AnalogOutputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager
) : IAnalogOutputFactory
{
    public IAnalogOutput Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeAnalogOutput()
            : new AnalogOutput(deviceManager);
    }
}
