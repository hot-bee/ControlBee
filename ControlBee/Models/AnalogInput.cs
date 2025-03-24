using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class AnalogInput(IDeviceManager deviceManager) : AnalogIO(deviceManager), IAnalogInput
{
    private long _data;

    protected long InternalData
    {
        get => _data;
        set
        {
            if (SetField(ref _data, value))
                SendDataToUi(Guid.Empty);
        }
    }

    public long Read()
    {
        ReadFromDevice();
        return InternalData;
    }

    protected virtual void ReadFromDevice()
    {
        // TODO
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["Data"] = InternalData };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
                throw new ValueError();
        }
        return base.ProcessMessage(message);
    }
}
