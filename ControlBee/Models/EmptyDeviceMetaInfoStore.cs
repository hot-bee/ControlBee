using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyDeviceMetaInfoStore : IDeviceMetaInfoStore
{
    private EmptyDeviceMetaInfoStore() { }

    public static EmptyDeviceMetaInfoStore Instance { get; } = new();

    public DeviceMetaInfo Get(string deviceName)
    {
        return new DeviceMetaInfo();
    }
}
