namespace ControlBee.Interfaces;

public interface IDigitalInput : IDigitalIO
{
    bool IsOn { get; }
    bool IsOff { get; }

    void WaitOn();
    void WaitOff();
    void WaitOn(int millisecondsTimeout);
    void WaitOff(int millisecondsTimeout);
}
