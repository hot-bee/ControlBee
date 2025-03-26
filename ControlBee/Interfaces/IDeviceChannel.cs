using ControlBeeAbstract.Devices;

namespace ControlBee.Interfaces;

public interface IDeviceChannel : IActorItem
{
    void RefreshCache();
    IDevice? GetDevice();
    int GetChannel();
}
