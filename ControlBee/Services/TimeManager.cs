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

    public Task TaskRun(Action action)
    {
        return Task.Run(action);
    }
}
