using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalInput(IDeviceManager deviceManager) : DigitalIO(deviceManager), IDigitalInput
{
    protected bool InternalIsOn;
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
            WaitSensor(() => IsOn, millisecondsTimeout);
        }
        catch (TimeoutError error)
        {
            IsOnTimeout.Trigger();
            throw;
        }
    }

    public void WaitOff(int millisecondsTimeout)
    {
        try
        {
            WaitSensor(() => IsOff, millisecondsTimeout);
        }
        catch (TimeoutError error)
        {
            IsOffTimeout.Trigger();
            throw;
        }
    }

    protected virtual void WaitSensor(Func<bool> sensorValue, int millisecondsTimeout)
    {
        var watch = TimeManager.CreateWatch();
        while (true)
        {
            if (sensorValue())
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

    public virtual void ReadFromDevice()
    {
        throw new NotImplementedException();
    }
}
