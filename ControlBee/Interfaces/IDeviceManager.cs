using ControlBeeAbstract.Devices;

namespace ControlBee.Interfaces;

public interface IDeviceManager
{
    IDevice? Get(string name);
    void Add(string name, IDevice device);
}
