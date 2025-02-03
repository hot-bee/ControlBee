using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Utils;

public class DictPath
{
    private DictPath(object? internalObject)
    {
        Value = internalObject;
    }

    public DictPath this[string key] =>
        Value is Dict nestedDict
            ? new DictPath(nestedDict.GetValueOrDefault(key))
            : new DictPath(null);

    public object? Value { get; }

    public static DictPath Start(object? dictObject)
    {
        return new DictPath(dictObject);
    }
}
