using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;

namespace ControlBee.Models;

public class EmptyDeviceManager : IDeviceManager
{
    private EmptyDeviceManager() { }

    public static EmptyDeviceManager Instance { get; } = new();

    public IDevice? Get(string name)
    {
        throw new UnimplementedByDesignError();
    }

    public void Add(string name, IDevice device)
    {
        throw new NotImplementedException();
    }
}
