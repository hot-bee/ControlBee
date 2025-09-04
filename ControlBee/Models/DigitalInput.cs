﻿using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalInput));

    private bool _isOn;

    #region Cache

    private bool _isOnCache;

    #endregion

    protected bool InternalIsOn
    {
        get => _isOn;
        set
        {
            if (SetField(ref _isOn, value))
                SendDataToUi(Guid.Empty);
        }
    }

    protected virtual IDigitalIoDevice? DigitalIoDevice => Device as IDigitalIoDevice;
    protected bool Reversed { get; private set; }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Reversed)) is string reversedValue)
            if (bool.TryParse(reversedValue, out var reversed))
                Reversed = reversed;
    }

    public bool IsOn()
    {
        if (DigitalIoDevice == null)
            //Logger.Warn("DigitalIoDevice is null.");
            return InternalIsOn;

        var inputValue = DigitalIoDevice.GetDigitalInputBit(Channel);
        if (Reversed) inputValue = !inputValue;
        InternalIsOn = inputValue;
        return InternalIsOn;
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
        base.RefreshCache();

        if (DigitalIoDevice == null)
            return;
        RefreshCacheImpl();
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
        var payload = new Dict { [nameof(IsOn)] = IsOn() };
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

    #region Timeouts

    public Variable<int> OffTimeout = new(VariableScope.Global, 5000);
    public Variable<int> OnTimeout = new(VariableScope.Global, 5000);

    #endregion

    #region Dialogs

    public IDialog OffTimeoutError = new DialogPlaceholder();
    public IDialog OnTimeoutError = new DialogPlaceholder();

    #endregion
}