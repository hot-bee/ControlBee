using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IDeviceMetaInfoStore
{
    DeviceMetaInfo Get(string deviceName);
}
