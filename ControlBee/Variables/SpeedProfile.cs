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

    public SpeedProfile()
    {
        
    }

    protected SpeedProfile(SpeedProfile source) : base(source)
    {
        Velocity = source.Velocity;
        Accel = source.Accel;
        Decel = source.Decel;
        AccelJerkRatio = source.AccelJerkRatio;
        DecelJerkRatio = source.DecelJerkRatio;
    }

    public double Velocity
    {
        get => _velocity;
        set => ValueChangedUtils.SetField(ref _velocity, value, OnValueChanging, OnValueChanged);
    }

    public double Accel
    {
        get => _accel;
        set => ValueChangedUtils.SetField(ref _accel, value, OnValueChanging, OnValueChanged);
    }

    public double Decel
    {
        get => _decel;
        set => ValueChangedUtils.SetField(ref _decel, value, OnValueChanging, OnValueChanged);
    }

    public double AccelJerkRatio
    {
        get => _accelJerkRatio;
        set => ValueChangedUtils.SetField(ref _accelJerkRatio, value, OnValueChanging, OnValueChanged);
    }

    public double DecelJerkRatio
    {
        get => _decelJerkRatio;
        set => ValueChangedUtils.SetField(ref _decelJerkRatio, value, OnValueChanging, OnValueChanged);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public double AccelJerk
    {
        get
        {
            var accelTime = Velocity / Accel;
            var jerk = Velocity / AccelJerkRatio / (accelTime * accelTime);
            return jerk;
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
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
        return new SpeedProfile(this);
    }

    public override void OnDeserialized()
    {
        // Empty
    }
}