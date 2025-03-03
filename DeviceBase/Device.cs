using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace DeviceBase;

public abstract class Device : IDevice
{
    public abstract void Init(Dict config);
    public abstract void Dispose();
}
