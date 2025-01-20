using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyDeviceManager : IDeviceManager
{
    public IDevice GetDevice(string deviceName)
    {
        throw new UnimplementedByDesignError();
    }
}
