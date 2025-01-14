using ControlBee.Interfaces;

namespace ControlBee.Utils;

public class FrozenStopwatch(IFrozenTimeManager frozenTimeManager) : IStopwatch
{
    private int _startTime;

    public void Start()
    {
        _startTime = frozenTimeManager.CurrentMilliseconds;
    }

    public long ElapsedMilliseconds => frozenTimeManager.CurrentMilliseconds - _startTime;
    public double ElapsedSeconds => ElapsedMilliseconds / 1000.0;

    public void Restart()
    {
        Start();
    }
}
