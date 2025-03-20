namespace ControlBee.Utils;

public class GuidUtils
{
    public static Guid FromObject(object? obj)
    {
        if (obj is Guid guid)
            return guid;
        return Guid.Empty;
    }
}
