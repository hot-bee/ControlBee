namespace ControlBee.Interfaces;

public interface IDeviceMonitor
{
    void Add(IDeviceChannel channel);
    void Start();
}
