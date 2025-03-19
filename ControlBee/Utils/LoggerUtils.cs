using System.Diagnostics;

namespace ControlBee.Utils;

public class LoggerUtils
{
    public static string CurrentStackDefaultLog()
    {
        var currentStack = new StackTrace(true);
        return currentStack.ToString();
    }
}
