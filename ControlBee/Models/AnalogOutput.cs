using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class AnalogOutput(IDeviceManager deviceManager) : AnalogIO(deviceManager), IAnalogOutput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(AnalogOutput));
    protected long InternalData;
    protected virtual IAnalogIoDevice? AnalogIoDevice => Device as IAnalogIoDevice;

    public void Write(long data)
    {
        InternalData = data;
        SendDataToUi(Guid.Empty);
        if (AnalogIoDevice == null)
        {
            Logger.Warn("AnalogIoDevice is null.");
            return;
        }

        AnalogIoDevice.SetAnalogOutputSignedDWord(Channel, (int)InternalData);  // TODO: Separate this according to the data size that will be defined in property.
    }

    public long Read()
    {
        return InternalData;
    }

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
