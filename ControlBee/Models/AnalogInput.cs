using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class AnalogInput(IDeviceManager deviceManager) : AnalogIO(deviceManager), IAnalogInput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(AnalogInput));
    private long _data;
    protected virtual IAnalogIoDevice? AnalogIoDevice => Device as IAnalogIoDevice;
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
        if (AnalogIoDevice == null)
        {
            Logger.Warn("AnalogIoDevice is null.");
            return InternalData;
        }

        InternalData = AnalogIoDevice.GetAnalogInputSignedDWord(Channel);  // TODO: Separate this according to the data size that will be defined in property.
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
