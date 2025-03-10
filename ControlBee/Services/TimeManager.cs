﻿using ControlBee.Interfaces;
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

    public Task<T> RunTask<T>(Func<T> func)
    {
        return Task.Run<T>(func);
    }

    public int CurrentMilliseconds => 0;
    public event EventHandler<int>? CurrentTimeChanged;

    protected virtual void OnCurrentTimeChanged(int e)
    {
        CurrentTimeChanged?.Invoke(this, e);
    }

    public void Dispose() { }
}
