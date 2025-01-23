using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    private bool _isOn;
    protected bool InternalIsOn
    {
        get => _isOn;
        set
        {
            if (_isOn.Equals(value))
                return;
            _isOn = value;
            SendDataToUi(Guid.Empty);
        }
    }

    public Alert IsOffTimeout = new();
    public Alert IsOnTimeout = new();

    public override void UpdateSubItem() { }

    public bool IsOn
    {
        get
        {
            ReadFromDevice();
            return InternalIsOn;
        }
    }

    public bool IsOff => !IsOn;

    public void WaitOn()
    {
        WaitOn(0);
    }

    public void WaitOff()
    {
        WaitOff(0);
    }

    public virtual void WaitOn(int millisecondsTimeout)
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
            if (IsOn == isOn)
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

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dictionary<string, object?> { [nameof(IsOn)] = IsOn };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    public virtual void ReadFromDevice()
    {
        throw new NotImplementedException();
    }
}
