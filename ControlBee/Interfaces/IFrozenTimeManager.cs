namespace ControlBee.Interfaces;

public interface IFrozenTimeManager : ITimeManager
{
    int CurrentMilliseconds { get; }
    event EventHandler<int> CurrentTimeChanged;
}
