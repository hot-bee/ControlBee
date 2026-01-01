namespace ControlBee.Interfaces;

public interface IDeviceMonitor : IDisposable
{
    void Add(IDeviceChannel channel);
    void Start();
    void SetAlwaysUpdate(string actorName, string itemPath);
    bool GetAlwaysUpdate(string actorName, string itemPath);
}
