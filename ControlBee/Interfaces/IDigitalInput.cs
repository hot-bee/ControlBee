namespace ControlBee.Interfaces;

public interface IDigitalInput : IDigitalIO
{
    bool IsOn();
    bool IsOff();
    bool IsOnOrSet();
    bool IsOffOrSet();

    void WaitOn();
    void WaitOff();
}
