using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBeeAbstract.Devices;
using log4net;

namespace ControlBee.Models;

public class DeviceManager : IDeviceManager, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DeviceManager));
    private readonly Dictionary<string, IDevice> _devices = [];
    private readonly IDeviceMetaInfoStore _deviceMetaInfoStore;

    public DeviceManager()
    {
        _deviceMetaInfoStore = new DeviceMetaInfoStore();
    }

    public DeviceManager(IDeviceMetaInfoStore deviceMetaInfoStore)
    {
        _deviceMetaInfoStore = deviceMetaInfoStore;
    }

    public IDevice? Get(string name)
    {
        return _devices.GetValueOrDefault(name);
    }

    public void Add(string name, IDevice device)
    {
        _devices.Add(name, device);
    }

    public void Dispose()
    {
        foreach (var (name, device) in _devices)
            device.Dispose();
        _devices.Clear();
    }

    public IDevice[] GetDevices()
    {
        return _devices.Values.ToArray();
    }

    public DeviceMetaInfo GetDeviceMetaInfo(string deviceName)
    {
        return _deviceMetaInfoStore.Get(deviceName);
    }

    public void AbortAll()
    {
        foreach (var name in _devices.Keys)
        {
            try
            {
                GetDeviceMetaInfo(name).Abort();
            }
            catch (Exception exception)
            {
                Logger.Error($"Failed to abort device. ({name})", exception);
            }
        }
    }
}
