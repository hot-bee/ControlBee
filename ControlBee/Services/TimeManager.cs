using ControlBee.Interfaces;
using ControlBee.Utils;

namespace ControlBee.Services;

public class TimeManager : ITimeManager
{
    public void Sleep(int millisecondsTimeout)
    {
        Thread.Sleep(millisecondsTimeout);
    }

    public IStopwatch CreateWatch()
    {
        return Stopwatch.Create();
    }
}
