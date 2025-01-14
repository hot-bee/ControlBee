using ControlBee.Interfaces;
using ControlBee.Utils;

namespace ControlBee.Services;

public class FrozenTimeManager(int tickMilliseconds) : IFrozenTimeManager
{
    public void Sleep(int millisecondsTimeout)
    {
        var startTime = CurrentMilliseconds;

        while (true)
        {
            if (startTime + millisecondsTimeout <= CurrentMilliseconds)
                break;
            Tick(tickMilliseconds);
        }
    }

    public IStopwatch CreateWatch()
    {
        return new FrozenStopwatch(this);
    }

    public int CurrentMilliseconds { get; private set; }
    public event EventHandler<int>? CurrentTimeChanged;

    public void Tick(int elapsedMilliseconds)
    {
        CurrentMilliseconds += elapsedMilliseconds;
        OnCurrentTimeChanged(elapsedMilliseconds);
    }

    protected virtual void OnCurrentTimeChanged(int e)
    {
        CurrentTimeChanged?.Invoke(this, e);
    }
}
