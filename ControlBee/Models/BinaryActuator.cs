using System.ComponentModel;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class BinaryActuator : ActorItem, IBinaryActuator
{
    private IDigitalInput? _inputOff;
    private IDigitalInput? _inputOn;
    private bool _on;
    private IDigitalOutput? _outputOff;
    private IDigitalOutput _outputOn;

    public BinaryActuator(
        IDigitalOutput outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    )
    {
        _outputOn = outputOn;
        _outputOff = outputOff;
        _inputOn = inputOn;
        _inputOff = inputOff;
        Subscribe();
    }

    public bool On
    {
        get => _on;
        set
        {
            _on = value;
            _outputOn.On = _on;
            if (_outputOff != null)
                _outputOff.On = !_on;
            SendDataToUi(Guid.Empty);
        }
    }

    public bool Off
    {
        get => !On;
        set => On = !value;
    }

    public bool IsOn
    {
        get
        {
            if (Off)
                return false;
            return _inputOn == null || _inputOn.IsOn;
        }
    }

    public bool IsOff
    {
        get
        {
            if (On)
                return false;
            return _inputOff == null || _inputOff.IsOn;
        }
    }

    public bool InputOnValue => _inputOn?.IsOn ?? false;
    public bool InputOffValue => _inputOff?.IsOn ?? false;

    public void OnAndWait(int millisecondsTimeout)
    {
        On = true;
        _inputOff?.WaitOff(millisecondsTimeout);
        _inputOn?.WaitOn(millisecondsTimeout);
    }

    public void OffAndWait(int millisecondsTimeout)
    {
        On = false;
        _inputOn?.WaitOff(millisecondsTimeout);
        _inputOff?.WaitOn(millisecondsTimeout);
    }

    public override void UpdateSubItem() { }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
                On = (bool)message.DictPayload!["On"]!;
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
        Subscribe();
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
            [nameof(On)] = On,
            [nameof(IsOn)] = IsOn,
            [nameof(InputOffValue)] = InputOffValue,
            [nameof(InputOnValue)] = InputOnValue,
        };
        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }
}
