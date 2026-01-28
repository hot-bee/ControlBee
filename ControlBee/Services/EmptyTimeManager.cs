using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Services;

public class EmptyTimeManager : ITimeManager
{
    public static EmptyTimeManager Instance = new EmptyTimeManager();

    private EmptyTimeManager() { }

    public void Sleep(int millisecondsTimeout)
    {
        throw new SystemException("Do not reach here.");
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

    public Task<T> RunTask<T>(Func<T> func)
    {
        return Task.Run(func);
    }

    public int CurrentMilliseconds => 0;
    public event EventHandler<int>? CurrentTimeChanged;

    protected virtual void OnCurrentTimeChanged(int e)
    {
        CurrentTimeChanged?.Invoke(this, e);
    }

    public void Dispose() { }
}
