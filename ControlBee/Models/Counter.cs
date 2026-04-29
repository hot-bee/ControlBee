using ControlBee.Interfaces;
using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Devices;
using log4net;
using static System.Enum;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Counter(IDeviceManager deviceManager) : DeviceChannel(deviceManager), ICounter
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Counter));

    private double _count;

    #region Cache

    private double _countCache;

    #endregion

    public EncoderMode EncoderMode;

    protected virtual ICounterDevice? CounterDevice => Device as ICounterDevice;

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(EncoderMode)) is string encoderMode)
            TryParse(encoderMode, out EncoderMode);
    }

    public override void PostInit()
    {
        base.PostInit();
        Sync();
    }

    public override void Sync()
    {
        CounterDevice?.SetEncoderMode(Channel, EncoderMode);
    }

    public virtual void SetCounterValue(double value)
    {
        if (CounterDevice == null)
        {
            Logger.Debug("CounterDevice is null.");
            return;
        }

        SetCounterValueImpl(value);
    }

    public virtual double GetCounterValue()
    {
        if (CounterDevice == null)
            return _count;

        _count = CounterDevice.GetCounterValue(Channel);
        return _count;
    }

    protected void SetCounterValueImpl(double value)
    {
        _count = value;
        CounterDevice?.SetCounterValue(Channel, value);
        SendDataToUi(Guid.Empty);
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
                    var count = (int)message.DictPayload!["Count"]!;
                    SetCounterValue(count);
                    return true;
                }
        }

        return base.ProcessMessage(message);
    }

    public override void RefreshCache(bool alwaysUpdate = false)
    {
        base.RefreshCache(alwaysUpdate);

        if (CounterDevice == null)
            return;
        RefreshCacheImpl();
    }

    protected void RefreshCacheImpl()
    {
        var count = GetCounterValue();

        var updated = false;
        lock (this)
        {
            updated |= UpdateCache(ref _countCache, count);
        }

        if (updated)
            SendDataToUi(Guid.Empty);
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["Count"] = _count };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    private static bool UpdateCache<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        return true;
    }
}
