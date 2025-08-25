using System.Reflection;
using ControlBee.Utils;
using log4net;

namespace ControlBee.Variables;

public class SpeedProfile : PropertyVariable, ICloneable
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(SpeedProfile));
    private double _accel;
    private double _accelJerkRatio;
    private double _decel;
    private double _decelJerkRatio;
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

    public double AccelJerkRatio
    {
        get => _accelJerkRatio;
        set => ValueChangedUtils.SetField(ref _accelJerkRatio, value, OnValueChanged);
    }

    public double DecelJerkRatio
    {
        get => _decelJerkRatio;
        set => ValueChangedUtils.SetField(ref _decelJerkRatio, value, OnValueChanged);
    }

    public double AccelJerk
    {
        get
        {
            var accelTime = Velocity / Accel;
            var jerk = Velocity / AccelJerkRatio / (accelTime * accelTime);
            return jerk;
        }
    }

    public double DecelJerk
    {
        get
        {
            var decelTime = Velocity / Decel;
            var jerk = Velocity / DecelJerkRatio / (decelTime * decelTime);
            return jerk;
        }
    }

    public object Clone()
    {
        var clone = MemberwiseClone();
        typeof(SpeedProfile).GetField("_valueChanged", 
            BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(clone, null);
        return clone;
    }

    public override void OnDeserialized()
    {
        // Empty
    }
}