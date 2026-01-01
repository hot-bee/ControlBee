using ControlBeeAbstract.Devices;

namespace ControlBee.Interfaces;

public interface IDeviceChannelModifier
{
    void SetChannel(int channel);
    void SetDevice(string deviceName);
    void Sync();
}
