namespace ControlBee.Models;

public class DeviceChannelInfo(string deviceName, int channelId)
{
    public string DeviceName { get; } = deviceName;
    public int ChannelId { get; } = channelId;
}
