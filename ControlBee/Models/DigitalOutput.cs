using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DigitalOutput(IDeviceManager deviceManager, ITimeManager timeManager)
    : DigitalIO(deviceManager),
        IDigitalOutput
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalOutput));
    private bool? _actualOn;
    private bool _commandOn;
    private Task? _task;
    public Variable<int> OffDelay = new(VariableScope.Global, 0);
    public Variable<int> OnDelay = new(VariableScope.Global, 0);

    protected virtual IDigitalIoDevice? DigitalIoDevice => Device as IDigitalIoDevice;

    protected bool CommandOn
    {
        get => _commandOn;
        set
        {
            if (_commandOn.Equals(value)) return;
            _commandOn = value;
            OnCommandOnChanged(_commandOn);
            SendDataToUi(Guid.Empty);
        }
    }

    protected bool? ActualOn
    {
        get => _actualOn;
        set
        {
            if (Equals(_actualOn, value)) return;
            _actualOn = value;
            OnActualOnChanged(_actualOn);
            SendDataToUi(Guid.Empty);
        }
    }

    public event EventHandler<bool>? CommandOnChanged;
    public event EventHandler<bool?>? ActualOnChanged;

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

    protected virtual void SetOnImpl(bool on)
    {
        if (CommandOn == on)
            return;
        CommandOn = on;
        DigitalIoDevice?.SetDigitalOutputBit(Channel, on);
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

            ActualOn = CommandOn;
        });
    }
    public virtual void SetOn(bool on)
    {
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }

        SetOnImpl(on);
    }

    public void On()
    {
        SetOn(true);
    }

    public void Off()
    {
        SetOn(false);
    }

    public bool? IsOn(CommandActualType type = CommandActualType.Actual)
    {
        switch (type)
        {
            case CommandActualType.Command:
                return CommandOn;
            case CommandActualType.Actual:
                if (_task is { IsCompleted: true })
                    _task = null;
                return ActualOn;
        }

        throw new ValueError();
    }

    public bool? IsOff(CommandActualType type = CommandActualType.Actual)
    {
        return !IsOn(type);
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

    public override void PostInit()
    {
        base.PostInit();
        Sync();
    }

    private void DigitalIoDeviceOnOutputBitChanged(object? sender, (int channel, bool value) e)
    {
        if (e.channel != Channel) return;
        if (CommandOn == e.value) return;
        CommandOn = e.value;
        ActualOn = e.value;
    }

    public override void Sync()
    {
        if (DigitalIoDevice == null)
        {
            Logger.Warn("DigitalIoDevice is null.");
            return;
        }

        CommandOn = DigitalIoDevice.GetDigitalOutputBit(Channel);
        ActualOn = CommandOn;
        if (DigitalIoDevice != null) DigitalIoDevice.OutputBitChanged += DigitalIoDeviceOnOutputBitChanged;
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dict { ["CommandOn"] = CommandOn, ["ActualOn"] = IsOn() };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    protected virtual void OnCommandOnChanged(bool e)
    {
        CommandOnChanged?.Invoke(this, e);
    }

    protected virtual void OnActualOnChanged(bool? e)
    {
        ActualOnChanged?.Invoke(this, e);
    }
}