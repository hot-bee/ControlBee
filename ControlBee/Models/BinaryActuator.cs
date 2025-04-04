using System.ComponentModel;
using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class BinaryActuator : ActorItem, IBinaryActuator
{
    private readonly IScenarioFlowTester _scenarioFlowTester;
    private readonly SystemConfigurations _systemConfigurations;
    private readonly ITimeManager _timeManager;
    private IDigitalInput? _inputOff;
    private IDigitalInput? _inputOn;

    private bool? _isOn;

    private bool _on;
    private IDigitalOutput? _outputOff;
    private IDigitalOutput? _outputOn;

    private Task<bool>? _task;
    public IDialog TimeoutError = new DialogPlaceholder();

    public BinaryActuator(
        SystemConfigurations systemConfigurations,
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

    public bool GetCommandOn()
    {
        return _on;
    }

    public bool GetCommandOff()
    {
        return !_on;
    }

    public void On()
    {
        SetOn(true);
    }

    public bool? IsOn()
    {
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

        return _isOn;
        // ReSharper restore InvertIf
    }

    public bool? IsOff()
    {
        return !IsOn();
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
        SetOn(true);
        Wait();
    }

    public void OffAndWait()
    {
        SetOn(false);
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
        _isOn = null;
        _on = on;
        _outputOn?.SetOn(_on);
        _outputOff?.SetOn(!_on);
        if (_systemConfigurations.SkipWaitSensor)
        {
            if (_inputOn is FakeDigitalInput fakeInputOn)
                fakeInputOn.On = on;
            if (_inputOff is FakeDigitalInput fakeInputOff)
                fakeInputOff.On = !on;
        }

        var delay = on ? OnTimeout.Value : OffTimeout.Value;
        _task = TimeManager.RunTask(() =>
        {
            var watch = _timeManager.CreateWatch();
            while (true)
            {
                if (_on && OnDetect())
                    break;
                if (!_on && OffDetect())
                    break;
                if (_inputOn == null && _inputOff == null)
                    break;
                if (watch.ElapsedMilliseconds >= delay)
                    return false;

                _timeManager.Sleep(1);
                _scenarioFlowTester.OnCheckpoint();
            }

            _isOn = _on;
            SendDataToUi(Guid.Empty);
            return true;
        });

        SendDataToUi(Guid.Empty);
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
            [nameof(On)] = _on,
            [nameof(IsOn)] = _isOn, // Do not call IsOn, or it will cause a recursive call issue.
            [nameof(OffDetect)] = OffDetect(),
            [nameof(OnDetect)] = OnDetect(),
        };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    #region Timeouts

    public Variable<int> OffTimeout = new(VariableScope.Global, 5000);
    public Variable<int> OnTimeout = new(VariableScope.Global, 5000);

    #endregion
}
