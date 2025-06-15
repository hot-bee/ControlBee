namespace ControlBee.Interfaces;

public interface IDeviceMonitor: IDisposable
{
    void Add(IDeviceChannel channel);
    void Start();
}
