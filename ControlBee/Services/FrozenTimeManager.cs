using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;

namespace ControlBee.Services;

public class FrozenTimeManager : ITimeManager
{
    private readonly IScenarioFlowTester _scenarioFlowTester;
    private readonly FrozenTimeManagerConfig _config;

    private readonly Dictionary<Thread, FrozenTimeManagerEvent> _threadEvents = new();
    private readonly Thread? _tickingThread;

    private bool _disposing;

    public FrozenTimeManager(
        SystemConfigurations systemConfigurations,
        IScenarioFlowTester scenarioFlowTester
    )
        : this(
            new FrozenTimeManagerConfig { EmulationMode = systemConfigurations.TimeEmulationMode },
            scenarioFlowTester
        ) { }

    public FrozenTimeManager(FrozenTimeManagerConfig config, IScenarioFlowTester scenarioFlowTester)
    {
        _config = config;
        _scenarioFlowTester = scenarioFlowTester;
        if (!config.ManualMode)
        {
            _tickingThread = new Thread(() =>
            {
                while (!_disposing)
                {
                    List<FrozenTimeManagerEvent> events;
                    lock (_threadEvents)
                    {
                        events = _threadEvents.Values.ToList();
                    }

                    if (events.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    var sleepingEvents = events.Where(x => x.IsSleeping).ToList();
                    if (sleepingEvents.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    var activeEvents = events
                        .Except(sleepingEvents)
                        .Where(x => (x.Thread.ThreadState & ThreadState.WaitSleepJoin) == 0)
                        .ToList();
                    if (activeEvents.Count > 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    sleepingEvents.ForEach(x => x.IsSleeping = false);

                    _tick(config.TickMilliseconds);
                    sleepingEvents.ForEach(x => x.ResumeEvent.Set());
                    var resumedEvents = sleepingEvents
                        .ToList()
                        .ConvertAll(x => x.ResumedEvent)
                        .ToArray<WaitHandle>();
                    WaitHandle.WaitAll(resumedEvents);
                }
            })
            {
                Name = "FrozenTimeManager_TickingThread",
            };
            _tickingThread.Start();
        }
    }

    public int RegisteredThreadsCount
    {
        get
        {
            lock (_threadEvents)
            {
                return _threadEvents.Count;
            }
        }
    }

    public void Dispose()
    {
        if (_tickingThread != null)
        {
            _disposing = true;
            _tickingThread.Join();
        }
    }

    public void Sleep(int millisecondsTimeout)
    {
        var startTime = CurrentMilliseconds;

        while (true)
        {
            if (startTime + millisecondsTimeout <= CurrentMilliseconds)
                break;
            FrozenTimeManagerEvent threadEvent;
            lock (_threadEvents)
            {
                threadEvent = _threadEvents[Thread.CurrentThread];
            }

            threadEvent.IsSleeping = true;
            threadEvent.ResumeEvent.WaitOne();
            threadEvent.ResumedEvent.Set();
        }
        _scenarioFlowTester.OnCheckpoint();
    }

    public IStopwatch CreateWatch()
    {
        return new FrozenStopwatch(this);
    }

    public int CurrentMilliseconds { get; private set; }
    public event EventHandler<int>? CurrentTimeChanged;

    public void Register()
    {
        var thread = Thread.CurrentThread;
        lock (_threadEvents)
        {
            if (_threadEvents.ContainsKey(thread))
                return;
            _threadEvents[thread] = new FrozenTimeManagerEvent();
        }
    }

    public void Unregister()
    {
        var thread = Thread.CurrentThread;
        lock (_threadEvents)
        {
            if (!_threadEvents.ContainsKey(thread))
                return;
            _threadEvents.Remove(thread);
        }
    }

    public Task RunTask(Action action)
    {
        var task = Task.Run(() =>
        {
            Register();
            try
            {
                action();
            }
            finally
            {
                Unregister();
            }
        });
        return task;
    }

    public Task<T> RunTask<T>(Func<T> func)
    {
        var task = Task.Run(() =>
        {
            Register();
            var ret = func();
            Unregister();
            return ret;
        });
        return task;
    }

    private void _tick(int elapsedMilliseconds)
    {
        if (_config.EmulationMode)
            Thread.Sleep(elapsedMilliseconds);
        CurrentMilliseconds += elapsedMilliseconds;
        OnCurrentTimeChanged(elapsedMilliseconds);
    }

    public void Tick(int elapsedMilliseconds)
    {
        if (!_config.ManualMode)
            throw new PlatformException("This action is allowed only when ManualMode is enabled.");
        _tick(elapsedMilliseconds);
    }

    protected virtual void OnCurrentTimeChanged(int e)
    {
        CurrentTimeChanged?.Invoke(this, e);
    }
}
