using ControlBee.Interfaces;
using ControlBee.Utils;

namespace ControlBee.Variables;

public class SpeedProfile : IValueChanged, ICloneable
{
    private double _accel;
    private double _decel;
    private double _jerk;
    private double _velocity;

    public double Velocity
    {
        get => _velocity;
        set => ValueChangedUtils.SetField(ref _velocity, value, OnValueChanged);
    }

    public double Accel
    {
        get => _accel;
        set => ValueChangedUtils.SetField(ref _accel, value, OnValueChanged);
    }

    public double Decel
    {
        get => _decel;
        set => ValueChangedUtils.SetField(ref _decel, value, OnValueChanged);
    }

    public double Jerk
    {
        get => _jerk;
        set => ValueChangedUtils.SetField(ref _jerk, value, OnValueChanged);
    }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    protected virtual void OnValueChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}
