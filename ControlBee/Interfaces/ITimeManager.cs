using ControlBee.Utils;

namespace ControlBee.Interfaces;

public interface ITimeManager
{
    void Sleep(int millisecondsTimeout);
    IStopwatch CreateWatch();
}
