using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class DeviceChannel(IDeviceManager deviceManager) : ActorItem, IDeviceChannel
{
    private DeviceChannelInfo? _deviceChannelInfo;

    public virtual void RefreshCache()
    {
        // Implement this on override functions
    }

    public IDevice GetDevice()
    {
        if (_deviceChannelInfo == null)
            throw new PlatformException();
        var device = deviceManager.GetDevice(_deviceChannelInfo.DeviceName);
        return device;
    }
}
