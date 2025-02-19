namespace ControlBee.Interfaces;

public interface IDigitalInput : IDigitalIO
{
    bool IsOn();
    bool IsOff();
    bool IsOnOrTrue();
    bool IsOffOrTrue();
    bool IsOnOrFalse();
    bool IsOffOrFalse();

    void WaitOn();
    void WaitOff();
}
