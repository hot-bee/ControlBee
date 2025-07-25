﻿using ControlBee.Constants;
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
    private long _dataCache;

    public AnalogDataType DataType;
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

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (
            dataSource.GetValue(ActorName, ItemPath, nameof(DataType))
            is string analogDataType
        )
            Enum.TryParse(analogDataType, out DataType);
    }

    public long Read()
    {
        if (AnalogIoDevice == null)
            //Logger.Warn("AnalogIoDevice is null.");
            return InternalData;

        switch (DataType)
        {
            case AnalogDataType.SignedDWord:
                InternalData = AnalogIoDevice.GetAnalogInputSignedDWord(Channel);
                break;
            case AnalogDataType.DWord:
                InternalData = AnalogIoDevice.GetAnalogInputDWord(Channel);
                break;
            case AnalogDataType.SignedWord:
                InternalData = AnalogIoDevice.GetAnalogInputSignedWord(Channel);
                break;
            case AnalogDataType.Word:
                InternalData = AnalogIoDevice.GetAnalogInputWord(Channel);
                break;
            case AnalogDataType.SignedByte:
                InternalData = AnalogIoDevice.GetAnalogInputSignedByte(Channel);
                break;
            case AnalogDataType.Byte:
                InternalData = AnalogIoDevice.GetAnalogInputByte(Channel);
                break;
            default:
                throw new SequenceError();
        }

        return InternalData;
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

    public override void RefreshCache(bool alwaysUpdate = false)
    {
        base.RefreshCache();

        if (AnalogIoDevice == null)
            return;
        RefreshCacheImpl(alwaysUpdate);
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["Data"] = InternalData };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    protected void RefreshCacheImpl(bool alwaysUpdate = false)
    {
        Read();
        var updated = false;
        lock (this)
        {
            updated |= UpdateCache(ref _dataCache, InternalData);
        }

        if (updated || alwaysUpdate)
            SendDataToUi(Guid.Empty);
    }

    private static bool UpdateCache<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        return true;
    }
}