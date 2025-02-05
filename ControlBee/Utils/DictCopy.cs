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

    public static Dict Copy(Dictionary<object, object> source)
    {
        var copied = new Dict();
        foreach (var (key, value) in source)
            if (value is Dictionary<object, object> dictValue)
                copied[(string)key] = Copy(dictValue);
            else
                copied[(string)key] = value;
        return copied;
    }
}
