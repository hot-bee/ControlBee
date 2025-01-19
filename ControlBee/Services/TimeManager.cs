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
        return new Stopwatch();
    }

    public void Register()
    {
        // Empty
    }

    public void Unregister()
    {
        // Empty
    }

    public Task RunTask(Action action)
    {
        return Task.Run(action);
    }

    public int CurrentMilliseconds => 0;
    public event EventHandler<int>? CurrentTimeChanged;
}
