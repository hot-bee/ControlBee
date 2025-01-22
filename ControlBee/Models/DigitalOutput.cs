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
            SendToUi(Guid.Empty);
        }
    }

    public bool Off
    {
        get => !On;
        set => On = !value;
    }

    public override void UpdateSubItem() { }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendToUi(message.Id);
                return true;
        }

        return base.ProcessMessage(message);
    }

    private void SendToUi(Guid requestId)
    {
        var payload = new Dictionary<string, object> { [nameof(On)] = On };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    public virtual void WriteToDevice()
    {
        throw new NotImplementedException();
    }
}
