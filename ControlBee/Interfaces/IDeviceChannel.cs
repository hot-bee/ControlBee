using ControlBeeAbstract.Devices;

namespace ControlBee.Interfaces;

public interface IDeviceChannel : IActorItem
{
    void RefreshCache(bool alwaysUpdate = false);
    IDevice? GetDevice();
    int GetChannel();
}
