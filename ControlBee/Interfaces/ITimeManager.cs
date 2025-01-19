using ControlBee.Models;
using ControlBee.Utils;

namespace ControlBee.Interfaces;

public interface ITimeManager
{
    void Sleep(int millisecondsTimeout);
    IStopwatch CreateWatch();
    void Register();
    void Unregister();
    Task RunTask(Action action);
    int CurrentMilliseconds { get; }
    event EventHandler<int> CurrentTimeChanged;
}
