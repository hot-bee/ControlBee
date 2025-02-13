using ControlBee.Interfaces;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class AnalogOutput(IDeviceManager deviceManager) : AnalogIO(deviceManager), IAnalogOutput
{
    protected long InternalData;

    public void Write(long data)
    {
        InternalData = data;
        WriteToDevice();
        SendDataToUi(Guid.Empty);
    }

    public long Read()
    {
        return InternalData;
    }

    public override void UpdateSubItem() { }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["Data"] = InternalData };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    protected virtual void WriteToDevice()
    {
        throw new NotImplementedException();
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
            {
                var data = Convert.ToInt64(message.DictPayload!["Data"]!);
                Write(data);
                return true;
            }
        }

        return base.ProcessMessage(message);
    }
}
