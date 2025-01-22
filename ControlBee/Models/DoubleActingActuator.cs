using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DoubleActingActuator(
    IDigitalOutput outputOff,
    IDigitalOutput outputOn,
    IDigitalInput? inputOff,
    IDigitalInput? inputOn
) : ActorItem
{
    private bool _on;

    public bool On
    {
        get => _on;
        set
        {
            _on = value;
            outputOff.On = !_on;
            outputOn.On = _on;
        }
    }

    public bool Off
    {
        get => !On;
        set => On = !value;
    }

    public bool IsOn
    {
        get
        {
            if (Off)
                return false;
            return inputOn == null || inputOn.IsOn;
        }
    }

    public bool IsOff
    {
        get
        {
            if (On)
                return false;
            return inputOff == null || inputOff.IsOn;
        }
    }

    public void OnAndWait(int millisecondsTimeout)
    {
        On = true;
        inputOn?.WaitOn(millisecondsTimeout);
    }

    public void OffAndWait(int millisecondsTimeout)
    {
        On = false;
        inputOff?.WaitOn(millisecondsTimeout);
    }

    public override void UpdateSubItem() { }

    public override void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        // TODO
    }
}
