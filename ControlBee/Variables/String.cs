using ControlBee.Interfaces;
using ControlBee.Utils;

namespace ControlBee.Variables;

public class String : IValueChanged
{
    private string _value = string.Empty;

    public String() { }

    public String(string value)
        : this()
    {
        _value = value;
    }

    public string Value
    {
        get => _value;
        set => ValueChangedUtils.SetField(ref _value, value, OnValueChanged);
    }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    public override string ToString()
    {
        return Value;
    }

    protected virtual void OnValueChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
