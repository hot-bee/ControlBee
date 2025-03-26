using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using log4net;

namespace ControlBee.Models;

public abstract class DeviceChannel(IDeviceManager deviceManager) : ActorItem, IDeviceChannel
{
    private static readonly ILog Logger = LogManager.GetLogger("General");
    protected IDevice? Device { get; set; }
    protected string? DeviceName { get; set; }
    protected int Channel { get; set; } = -1;

    public virtual void RefreshCache()
    {
        // Implement this on override functions
    }

    public IDevice? GetDevice()
    {
        return Device;
    }

    public int GetChannel()
    {
        return Channel;
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(DeviceName)) is string deviceName)
            DeviceName = deviceName;
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Channel)) is string channelIdValue)
            if (int.TryParse(channelIdValue, out var channel))
                Channel = channel;

        if (string.IsNullOrEmpty(DeviceName))
        {
            Logger.Warn($"DeviceName is empty. ({ActorName}, {ItemPath})");
            return;
        }

        if (Channel == -1)
        {
            Logger.Warn($"Channel is empty. ({ActorName}, {ItemPath})");
            return;
        }

        Device = deviceManager.Get(DeviceName!);
    }
}
