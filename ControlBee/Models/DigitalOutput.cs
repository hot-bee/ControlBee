using ControlBee.Interfaces;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalOutput(IDeviceManager deviceManager, ITimeManager timeManager)
    : DigitalIO(deviceManager),
        IDigitalOutput
{
    private const int MillisecondsDelay = 100; // Temp
    private bool? _isOn;
    protected bool InternalOn;
    private Task? _task;

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
            {
                var on = (bool)message.DictPayload!["On"]!;
                SetOn(on);
                return true;
            }
        }

        return base.ProcessMessage(message);
    }

    public virtual void SetOn(bool on)
    {
        if (InternalOn == on)
            return;
        InternalOn = on;
        WriteToDevice();
        _task = TimeManager.RunTask(() =>
        {
            var watch = timeManager.CreateWatch();
            while (true)
            {
                if (watch.ElapsedMilliseconds > MillisecondsDelay)
                    break;

                timeManager.Sleep(1);
            }

            _isOn = InternalOn;
            SendDataToUi(Guid.Empty);
        });
        SendDataToUi(Guid.Empty);
    }

    public void On()
    {
        SetOn(true);
    }

    public void Off()
    {
        SetOn(false);
    }

    public bool? IsOn()
    {
        if (_task is { IsCompleted: true })
            _task = null;
        return _isOn;
    }

    public bool? IsOff()
    {
        return !IsOn();
    }

    public bool IsCommandOn()
    {
        return InternalOn;
    }

    public bool IsCommandOff()
    {
        return !InternalOn;
    }

    public void Wait()
    {
        if (_task == null)
            return;
        _task.Wait();
        _ = IsOn();
    }

    public void OnAndWait()
    {
        On();
        Wait();
    }

    public void OffAndWait()
    {
        Off();
        Wait();
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["On"] = InternalOn, ["IsOn"] = IsOn() };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    public virtual void WriteToDevice()
    {
        throw new NotImplementedException();
    }
}
