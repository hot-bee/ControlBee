using ControlBee.Utils;

namespace ControlBee.Interfaces;

public interface IStopwatch
{
    long ElapsedMilliseconds { get; }
    double ElapsedSeconds { get; }
    void Restart();
    void Start();
}
