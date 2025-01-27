using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalOutputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager
) : IDigitalOutputFactory
{
    public IDigitalOutput Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeDigitalOutput(deviceManager, timeManager)
            : new DigitalOutput(deviceManager, timeManager);
    }
}
