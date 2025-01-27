namespace ControlBee.Interfaces;

public interface IDigitalInput : IDigitalIO
{
    bool IsOn();
    bool IsOff();

    void WaitOn();
    void WaitOff();
}
