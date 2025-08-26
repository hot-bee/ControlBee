using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;

namespace ControlBee.Models;

public class DeviceManager : IDeviceManager, IDisposable
{
    private readonly Dictionary<string, IDevice> _devices = [];

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
}