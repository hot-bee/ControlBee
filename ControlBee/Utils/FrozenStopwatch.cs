using ControlBee.Interfaces;

namespace ControlBee.Utils;

public class FrozenStopwatch : IStopwatch
{
    private readonly IFrozenTimeManager _frozenTimeManager;
    private int _startTime;

    public FrozenStopwatch(IFrozenTimeManager frozenTimeManager)
    {
        _frozenTimeManager = frozenTimeManager;
        Start();
    }

    public void Start()
    {
        _startTime = _frozenTimeManager.CurrentMilliseconds;
    }

    public long ElapsedMilliseconds => _frozenTimeManager.CurrentMilliseconds - _startTime;
    public double ElapsedSeconds => ElapsedMilliseconds / 1000.0;

    public void Restart()
    {
        Start();
    }
}
