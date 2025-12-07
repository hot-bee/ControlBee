using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IBinaryActuator : IActorItem, IUsesPlaceholder
{
    bool? IsOn(CommandActualType type = CommandActualType.Actual);
    bool? IsOff(CommandActualType type = CommandActualType.Actual);
    public bool OnDetect();
    public bool OffDetect();
    void On();
    void Off();
    void OnAndWait();

    void OffAndWait();
    void SetOnAndWait(bool value);
    void Wait();
}