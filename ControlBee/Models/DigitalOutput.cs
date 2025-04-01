using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using log4net;
using log4net.Repository.Hierarchy;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalOutput(IDeviceManager deviceManager, ITimeManager timeManager)
    : DigitalIO(deviceManager),
        IDigitalOutput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalOutput));
    private bool? _isOn;
    private Task? _task;
    protected bool InternalOn;
    public Variable<int> OffDelay = new(VariableScope.Global, 0);
    public Variable<int> OnDelay = new(VariableScope.Global, 0);

    protected virtual IDigitalIoDevice? DigitalIoDevice => Device as IDigitalIoDevice;

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
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }
        if (InternalOn == on)
            return;
        InternalOn = on;

        DigitalIoDevice.SetDigitalOutputBit(Channel, on);
        var delay = on ? OnDelay.Value : OffDelay.Value;
        _task = TimeManager.RunTask(() =>
        {
            var watch = timeManager.CreateWatch();
            while (true)
            {
                if (watch.ElapsedMilliseconds >= delay)
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

    public override void PostInit()
    {
        base.PostInit();
        if (DigitalIoDevice == null)
        {   
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }
        InternalOn = DigitalIoDevice.GetDigitalOutputBit(Channel);
        _isOn = InternalOn;
    }
}
