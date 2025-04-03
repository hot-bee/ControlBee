using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class EmptyDeviceManager : IDeviceManager
{
    private EmptyDeviceManager() { }

    public static EmptyDeviceManager Instance { get; } = new();

    public IDevice? Get(string name)
    {
        return null;
    }

    public void Add(string name, IDevice device)
    {
        throw new NotImplementedException();
    }
}
