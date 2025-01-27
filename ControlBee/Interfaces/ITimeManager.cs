namespace ControlBee.Interfaces;

public interface ITimeManager : IDisposable
{
    int CurrentMilliseconds { get; }
    void Sleep(int millisecondsTimeout);
    IStopwatch CreateWatch();
    void Register();
    void Unregister();
    Task RunTask(Action action);
    Task<T> RunTask<T>(Func<T> func);
    event EventHandler<int> CurrentTimeChanged;
}
