using ControlBee.Exceptions;
using ControlBee.Interfaces;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    private const int MillisecondsTimeout = 5000;
    private bool _isOn;

    public Alert IsOffTimeout = new();
    public Alert IsOnTimeout = new();

    protected bool InternalIsOn
    {
        get => _isOn;
        set
        {
            if (SetField(ref _isOn, value))
                SendDataToUi(Guid.Empty);
        }
    }

    public override void UpdateSubItem() { }

    public bool IsOn()
    {
        ReadFromDevice();
        return InternalIsOn;
    }

    public bool IsOff()
    {
        return !IsOn();
    }

    public bool IsOnOrSet()
    {
        return IsOnOffOrSet(true);
    }

    public bool IsOffOrSet()
    {
        return IsOnOffOrSet(false);
    }

    protected virtual bool IsOnOffOrSet(bool on)
    {
        return IsOn();
    }

    public void WaitOn()
    {
        WaitOn(MillisecondsTimeout);
    }

    public void WaitOff()
    {
        WaitOff(MillisecondsTimeout);
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

    public void WaitOn(int millisecondsTimeout)
    {
        try
        {
            WaitSensor(true, millisecondsTimeout);
        }
        catch (TimeoutError)
        {
            IsOnTimeout.Trigger();
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
            IsOffTimeout.Trigger();
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
}
