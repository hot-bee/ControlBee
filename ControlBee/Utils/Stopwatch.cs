using ControlBee.Interfaces;

namespace ControlBee.Utils;

public class Stopwatch : IStopwatch
{
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();

    private Stopwatch()
    {
        _stopwatch.Start();
    }

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

    public void Restart()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    public void Start()
    {
        _stopwatch.Start();
    }

    public static Stopwatch Create()
    {
        return new Stopwatch();
    }
}
