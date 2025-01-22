using ControlBee.Interfaces;

namespace ControlBee.Models;

// ReSharper disable once InconsistentNaming
public abstract class DigitalIO(IDeviceManager deviceManager) : DeviceChannel(deviceManager)
{
    public override void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        // TODO
    }
}
