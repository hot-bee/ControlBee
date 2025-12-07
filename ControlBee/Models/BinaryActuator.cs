using System.ComponentModel;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class BinaryActuator : ActorItem, IBinaryActuator
{
    private readonly IScenarioFlowTester _scenarioFlowTester;
    private readonly ISystemConfigurations _systemConfigurations;
    private readonly ITimeManager _timeManager;
    private IDigitalInput? _inputOff;
    private IDigitalInput? _inputOn;

    private bool? _actualOn;
    private bool _commandOn;
    private IDigitalOutput? _outputOff;
    private IDigitalOutput? _outputOn;

    private Task<bool>? _task;
    public IDialog TimeoutError = new DialogPlaceholder();

    public BinaryActuator(
        ISystemConfigurations systemConfigurations,
        ITimeManager timeManager,
        IScenarioFlowTester scenarioFlowTester,
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    )
    {
        _systemConfigurations = systemConfigurations;
        _timeManager = timeManager;
        _scenarioFlowTester = scenarioFlowTester;
        _outputOn = outputOn;
        _outputOff = outputOff;
        _inputOn = inputOn;
        _inputOff = inputOff;
        Subscribe();
    }

    public bool? ActualOn
    {
        get => _actualOn;
        set
        {
            if (Equals(_actualOn, value)) return;
            _actualOn = value;
            SendDataToUi(Guid.Empty);
        }
    }

    public bool CommandOn
    {
        get => _commandOn;
        set
        {
            if (_commandOn == value) return;
            _commandOn = value;
            SendDataToUi(Guid.Empty);
        }
    }

    public void On()
    {
        SetOn(true);
    }

    public bool? IsOn(CommandActualType type = CommandActualType.Actual)
    {
        switch (type)
        {
            case CommandActualType.Command:
                return CommandOn;
            case CommandActualType.Actual:
                // ReSharper disable InvertIf
                if (_task is { IsCompleted: true })
                {
                    var success = _task.Result;
                    _task = null;
                    if (!success)
                    {
                        TimeoutError.Show();
                        throw new TimeoutError();
                    }
                }

                return ActualOn;
                // ReSharper restore InvertIf
        }

        throw new ValueError();
    }

    public bool? IsOff(CommandActualType type = CommandActualType.Actual)
    {
        return !IsOn(type);
    }

    public bool OnDetect()
    {
        return _inputOn?.IsOn() ?? _inputOff?.IsOff() ?? false;
    }

    public bool OffDetect()
    {
        return _inputOff?.IsOn() ?? _inputOn?.IsOff() ?? false;
    }

    public void OnAndWait()
    {
        SetOnAndWait(true);
    }

    public void OffAndWait()
    {
        SetOnAndWait(false);
    }

    public void SetOnAndWait(bool value)
    {
        SetOn(value);
        Wait();
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
                SetOn((bool)message.DictPayload!["On"]!);
                return true;
        }

        return base.ProcessMessage(message);
    }

    public void ReplacePlaceholder(PlaceholderManager manager)
    {
        Unsubscribe();
        _outputOn = manager.TryGet(_outputOn);
        _outputOff = manager.TryGet(_outputOff);
        _inputOn = manager.TryGet(_inputOn);
        _inputOff = manager.TryGet(_inputOff);

        if (_inputOn?.GetDevice() == null) _inputOn = null;
        if (_inputOff?.GetDevice() == null) _inputOff = null;
        if (_outputOn?.GetDevice() == null) _outputOn = null;
        if (_outputOff?.GetDevice() == null) _outputOff = null;
        Subscribe();
    }

    public void Off()
    {
        SetOn(false);
    }

    public void Wait()
    {
        if (_task == null)
            return;
        _task.Wait();
        _ = IsOn();
    }

    private void SetOn(bool on)
    {
        Wait();
        if (ActualOn == on) return;
        ActualOn = null;
        CommandOn = on;
        _outputOn?.SetOn(CommandOn);
        _outputOff?.SetOn(!CommandOn);
        if (_systemConfigurations.SkipWaitSensor)
        {
            if (_inputOn is FakeDigitalInput fakeInputOn)
                fakeInputOn.On = on;
            if (_inputOff is FakeDigitalInput fakeInputOff)
                fakeInputOff.On = !on;
        }

        var delay = on ? OnDelay.Value : OffDelay.Value;
        var timeout = on ? OnTimeout.Value : OffTimeout.Value;
        _task = TimeManager.RunTask(() =>
        {
            var watch = _timeManager.CreateWatch();
            while (true)
            {
                if (CommandOn && OnDetect())
                    break;
                if (!CommandOn && OffDetect())
                    break;
                if (_inputOn == null && _inputOff == null)
                    break;
                if (watch.ElapsedMilliseconds >= timeout)
                    return false;

                _timeManager.Sleep(1);
                _scenarioFlowTester.OnCheckpoint();
            }

            _timeManager.Sleep(delay);

            ActualOn = CommandOn;
            return true;
        });
    }

    private void Unsubscribe()
    {
        if (_inputOff != null)
            _inputOff.PropertyChanged -= InputOnPropertyChanged;
        if (_inputOn != null)
            _inputOn.PropertyChanged -= InputOnPropertyChanged;
    }

    private void Subscribe()
    {
        if (_inputOff != null)
            _inputOff.PropertyChanged += InputOnPropertyChanged;
        if (_inputOn != null)
            _inputOn.PropertyChanged += InputOnPropertyChanged;
    }

    private void InputOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SendDataToUi(Guid.Empty);
    }

    private void SendDataToUi(Guid requestId)
    {
        var payload = new Dictionary<string, object?>
        {
            ["CommandOn"] = CommandOn,
            ["ActualOn"] = ActualOn, // Do not call IsOn, or it will cause a recursive call issue.
            [nameof(OffDetect)] = OffDetect(),
            [nameof(OnDetect)] = OnDetect()
        };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    public override void PostInit()
    {
        base.PostInit();
        if (_outputOn != null)
        {
            OutputOnOnCommandOnChanged(null, _outputOn.IsOn(CommandActualType.Command) is true);
            _outputOn.CommandOnChanged += OutputOnOnCommandOnChanged;
        }
    }

    private void OutputOnOnCommandOnChanged(object? sender, bool e)
    {
        if (e == CommandOn) return;
        CommandOn = e;
        ActualOn = null;
    }

    #region Timeouts

    public Variable<int> OffTimeout = new(VariableScope.Global, 5000);
    public Variable<int> OnTimeout = new(VariableScope.Global, 5000);
    public Variable<int> OffDelay = new(VariableScope.Global);
    public Variable<int> OnDelay = new(VariableScope.Global);

    #endregion
}