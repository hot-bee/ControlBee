using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Utils;

public class DictCopy
{
    public static Dict Copy(Dict source)
    {
        var copied = source.ToDictionary();
        foreach (var (key, value) in copied)
            if (value is Dict dictValue)
                copied[key] = Copy(dictValue);

        return copied;
    }
}
