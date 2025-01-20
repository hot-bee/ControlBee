using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalOutput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalOutput
{
    protected bool InternalOn;

    public bool On
    {
        get => InternalOn;
        set
        {
            if (InternalOn == value)
                return;
            InternalOn = value;
            WriteToDevice();
        }
    }

    public bool Off
    {
        get => !On;
        set => On = !value;
    }

    public virtual void WriteToDevice()
    {
        throw new NotImplementedException();
    }

    public override void ProcessMessage(Message message) { }

    public override void UpdateSubItem() { }
}
