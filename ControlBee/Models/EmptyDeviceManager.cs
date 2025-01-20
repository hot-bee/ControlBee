using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyDeviceManager : IDeviceManager
{
    private EmptyDeviceManager() { }

    public static EmptyDeviceManager Instance { get; } = new();

    public IDevice GetDevice(string deviceName)
    {
        throw new UnimplementedByDesignError();
    }
}
