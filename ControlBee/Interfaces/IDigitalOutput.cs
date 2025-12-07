using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IDigitalOutput : IDigitalIO
{
    void SetOn(bool on);
    void On();
    void Off();
    bool? IsOn(CommandActualType type = CommandActualType.Actual);
    bool? IsOff(CommandActualType type = CommandActualType.Actual);
    void Wait();
    void OnAndWait();
    void OffAndWait();
    event EventHandler<bool>? CommandOnChanged;
    event EventHandler<bool?>? ActualOnChanged;
}
