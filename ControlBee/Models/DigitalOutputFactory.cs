using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalOutputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager
) : IDigitalOutputFactory
{
    public IDigitalOutput Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeDigitalOutput()
            : new DigitalOutput(deviceManager);
    }
}
