namespace ControlBee.Interfaces;

public interface IDigitalInput : IDigitalIO
{
    bool IsOn();
    bool IsOff();
    bool IsOnOrTrue();
    bool IsOffOrTrue();
    bool IsOnOrFalse();
    bool IsOffOrFalse();

    void WaitOn(bool showErrorDialog = true);
    void WaitOff(bool showErrorDialog = true);
    void WaitOn(int millisecondsTimeout, bool showErrorDialog);
    void WaitOff(int millisecondsTimeout, bool showErrorDialog);
}
