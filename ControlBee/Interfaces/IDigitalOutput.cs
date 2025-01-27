namespace ControlBee.Interfaces;

public interface IDigitalOutput : IDigitalIO
{
    void SetOn(bool on);
    void On();
    void Off();
    bool? IsOn();
    bool? IsOff();
    bool IsCommandOn();
    bool IsCommandOff();
    void Wait();
    void OnAndWait();
    void OffAndWait();
}
