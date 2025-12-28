using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalInput));

    private bool _actualOn;

    #region Cache

    private bool _isOnCache;

    #endregion

    protected bool ActualOn
    {
        get => _actualOn;
        set
        {
            if (_actualOn == value) return;
            _actualOn = value;
            OnActualOnChanged(_actualOn);
            SendDataToUi(Guid.Empty);
        }
    }

    protected virtual IDigitalIoDevice? DigitalIoDevice => Device as IDigitalIoDevice;
    protected bool Inverted { get; private set; }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, "Reversed") is string reversedValue) // For backward compatibility
            if (bool.TryParse(reversedValue, out var reversed))
                Inverted |= reversed;
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Inverted)) is string invertedValue)
            if (bool.TryParse(invertedValue, out var inverted))
                Inverted |= inverted;
    }

    public bool IsOn()
    {
        if (DigitalIoDevice == null)
            //Logger.Warn("DigitalIoDevice is null.");
            return ActualOn;

        var inputValue = DigitalIoDevice.GetDigitalInputBit(Channel);
        ActualOn = inputValue;
        return ActualOn;
    }

    public bool IsOff()
    {
        return !IsOn();
    }

    public bool IsOnOrTrue()
    {
        return IsOnOffOrValue(true);
    }

    public bool IsOffOrTrue()
    {
        return !IsOnOffOrValue(!true);
    }

    public bool IsOnOrFalse()
    {
        return IsOnOffOrValue(false);
    }

    public bool IsOffOrFalse()
    {
        return !IsOnOffOrValue(!false);
    }

    public void WaitOn(bool showErrorDialog = true)
    {
        WaitOn(OnTimeout.Value, showErrorDialog);
    }

    public void WaitOff(bool showErrorDialog = true)
    {
        WaitOff(OffTimeout.Value, showErrorDialog);
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
        base.RefreshCache(alwaysUpdate);

        if (DigitalIoDevice == null)
            return;
        RefreshCacheImpl();
    }

    public void WaitOn(int millisecondsTimeout, bool showErrorDialog)
    {
        try
        {
            WaitSensor(true, millisecondsTimeout);
        }
        catch (TimeoutError)
        {
            if (showErrorDialog) OnTimeoutError.Show();
            throw;
        }
    }

    public void WaitOff(int millisecondsTimeout, bool showErrorDialog)
    {
        try
        {
            WaitSensor(false, millisecondsTimeout);
        }
        catch (TimeoutError)
        {
            if (showErrorDialog) OffTimeoutError.Show();
            throw;
        }
    }

    public event EventHandler<bool>? ActualOnChanged;

    public override void PostInit()
    {
        base.PostInit();
        Sync();
    }

    protected virtual bool IsOnOffOrValue(bool on)
    {
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return on;
        }

        return IsOn();
    }

    protected virtual void WaitSensor(bool isOn, int millisecondsTimeout)
    {
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }

        var watch = TimeManager.CreateWatch();
        while (true)
        {
            if (IsOn() == isOn)
                return;
            if (millisecondsTimeout > 0 && watch.ElapsedMilliseconds > millisecondsTimeout)
                throw new TimeoutError();
            TimeManager.Sleep(1);
            OnAfterSleepWaitingSensor();
        }
    }

    protected virtual void OnAfterSleepWaitingSensor()
    {
        // Empty
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["ActualOn"] = IsOn() };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    protected void RefreshCacheImpl()
    {
        var isOn = IsOn();

        var updated = false;
        lock (this)
        {
            updated |= UpdateCache(ref _isOnCache, isOn);
        }

        if (updated)
            SendDataToUi(Guid.Empty);
    }

    private static bool UpdateCache<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        return true;
    }

    protected virtual void OnActualOnChanged(bool e)
    {
        ActualOnChanged?.Invoke(this, e);
    }

    public override void Sync()
    {
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }

        if (Inverted) DigitalIoDevice.SetDigitalInputBitInverted(Channel, Inverted);
        DigitalIoDevice.InputBitChanged += DigitalIoDeviceOnInputBitChanged;
    }

    private void DigitalIoDeviceOnInputBitChanged(object? sender, (int channel, bool value) e)
    {
        if (e.channel != Channel) return;
        RefreshCache();
    }

    #region Timeouts

    public Variable<int> OffTimeout = new(VariableScope.Global, 5000);
    public Variable<int> OnTimeout = new(VariableScope.Global, 5000);

    #endregion

    #region Dialogs

    public IDialog OffTimeoutError = new DialogPlaceholder();
    public IDialog OnTimeoutError = new DialogPlaceholder();

    #endregion
}