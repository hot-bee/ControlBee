namespace DeviceBase;

using Dict = Dictionary<string, object?>;

public interface IDevice : IDisposable
{
    void Init(Dict config);
}
