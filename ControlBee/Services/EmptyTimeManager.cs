using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Utils;

namespace ControlBee.Services;

public class EmptyTimeManager : ITimeManager
{
    public void Sleep(int millisecondsTimeout)
    {
        // Empty
    }

    public IStopwatch CreateWatch()
    {
        throw new UnimplementedByDesignError();
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

    protected virtual void OnCurrentTimeChanged(int e)
    {
        CurrentTimeChanged?.Invoke(this, e);
    }
}
