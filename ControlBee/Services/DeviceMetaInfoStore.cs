using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class DeviceMetaInfoStore : IDeviceMetaInfoStore
{
    private readonly Dictionary<string, DeviceMetaInfo> _map = [];
    private readonly object _lock = new();

    public DeviceMetaInfo Get(string deviceName)
    {
        lock (_lock)
        {
            if (!_map.TryGetValue(deviceName, out var metaInfo))
            {
                metaInfo = new DeviceMetaInfo();
                _map[deviceName] = metaInfo;
            }
            return metaInfo;
        }
    }
}
