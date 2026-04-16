using ControlBee.Interfaces;

namespace ControlBee.Models;

public class CounterFactory(
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IDeviceMonitor deviceMonitor
) : ICounterFactory
{
    public ICounter Create()
    {
        var counter = systemConfigurations.FakeMode
            ? new FakeCounter()
            : new Counter(deviceManager);
        deviceMonitor.Add(counter);
        return counter;
    }
}
