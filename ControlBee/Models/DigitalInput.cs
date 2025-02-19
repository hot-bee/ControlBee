using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    private bool _isOn;

    protected bool InternalIsOn
    {
        get => _isOn;
        set
        {
            if (SetField(ref _isOn, value))
                SendDataToUi(Guid.Empty);
        }
    }

    public bool IsOn()
    {
        ReadFromDevice();
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
        return IsOnOffOrValue(true);
    }

    public bool IsOnOrFalse()
    {
        return IsOnOffOrValue(false);
    }

    public bool IsOffOrFalse()
    {
        return IsOnOffOrValue(false);
    }

    public void WaitOn()
    {
        WaitOn(OnTimeout.Value);
    }

    public void WaitOff()
    {
        WaitOff(OffTimeout.Value);
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

    protected virtual bool IsOnOffOrValue(bool on)
    {
        return IsOn();
    }

    public void WaitOn(int millisecondsTimeout)
    {
        try
        {
            WaitSensor(true, millisecondsTimeout);
        }
        catch (TimeoutError)
        {
            OnTimeoutError.Show();
            throw;
        }
    }

    public void WaitOff(int millisecondsTimeout)
    {
        try
        {
            WaitSensor(false, millisecondsTimeout);
        }
        catch (TimeoutError)
        {
            OffTimeoutError.Show();
            throw;
        }
    }

    protected virtual void WaitSensor(bool isOn, int millisecondsTimeout)
    {
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

    protected virtual void ReadFromDevice()
    {
        throw new NotImplementedException();
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
