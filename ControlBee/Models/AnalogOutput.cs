using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class AnalogOutput(IDeviceManager deviceManager) : AnalogIO(deviceManager), IAnalogOutput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(AnalogOutput));
    public AnalogDataType DataType;
    protected object InternalData;
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

        switch (DataType)
        {
            case AnalogDataType.SignedDWord:
                AnalogIoDevice.SetAnalogOutputSignedDWord(Channel, (int)InternalData);
                break;
            case AnalogDataType.DWord:
                AnalogIoDevice.SetAnalogOutputDWord(Channel, (uint)InternalData);
                break;
            case AnalogDataType.SignedWord:
                AnalogIoDevice.SetAnalogOutputSignedWord(Channel, (short)InternalData);
                break;
            case AnalogDataType.Word:
                AnalogIoDevice.SetAnalogOutputWord(Channel, (ushort)InternalData);
                break;
            case AnalogDataType.SignedByte:
                AnalogIoDevice.SetAnalogOutputSignedByte(Channel, (sbyte)InternalData);
                break;
            case AnalogDataType.Byte:
                AnalogIoDevice.SetAnalogOutputByte(Channel, (byte)InternalData);
                break;
            default:
                throw new ValueError();
        }
    }

    public long Read()
    {
        return (long)InternalData;
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

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(DataType)) is string analogDataType)
            Enum.TryParse(analogDataType, out DataType);

        switch (DataType)
        {
            case AnalogDataType.Double:
                InternalData = 0.0;
                break;
            default:
                InternalData = (long)0;
                break;
        }
    }

    public double ReadDouble()
    {
        return (double)InternalData;
    }

    public void WriteDouble(double data)
    {
        InternalData = data;
        SendDataToUi(Guid.Empty);
        if (AnalogIoDevice == null)
        {
            Logger.Warn("AnalogIoDevice is null.");
            return;
        }

        if (DataType != AnalogDataType.Double)
            throw new ValueError($"DataType must be Double. (Channel: {Channel})");
        AnalogIoDevice.SetAnalogOutputDouble(Channel, data);
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
}
