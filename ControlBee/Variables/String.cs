using ControlBee.Utils;

namespace ControlBee.Variables;

public class String : PropertyVariable
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

    public override string ToString()
    {
        return Value;
    }

    public override void OnDeserialized()
    {
        // Empty
    }
}
