namespace ControlBee.Interfaces;

public interface IBinaryActuator : IActorItem, IUsesPlaceholder
{
    bool? IsOn();
    bool? IsOff();
    public bool OnDetect();
    public bool OffDetect();
    bool GetCommandOn();
    bool GetCommandOff();
    void On();
    void Off();
    void OnAndWait();

    void OffAndWait();
    void Wait();
}
