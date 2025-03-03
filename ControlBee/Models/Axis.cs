using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;
using DeviceBase;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Axis(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IAxis
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private Action? _initializeAction;
    private bool _initializing;

    public Variable<int> EnableDelay = new(VariableScope.Global, 200);

    public Variable<SpeedProfile> HomingSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    public Variable<SpeedProfile> JogSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    public Variable<SpeedProfile> OperationSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    protected SpeedProfile? SpeedProfile;

    public Variable<Array1D<double>> StepJogSizes = new(
        VariableScope.Global,
        new Array1D<double>([0.01, 0.1, 1.0])
    );

    // ReSharper disable once SuspiciousTypeConversion.Global
    protected virtual IMotionDevice? MotionDevice => Device as IMotionDevice;

    public override void RefreshCache()
    {
        base.RefreshCache();
        if (MotionDevice == null)
            return;
        var commandPosition = GetPosition(PositionType.Command);
        var actualPosition = GetPosition(PositionType.Actual);
        var isMoving = IsMoving();
        var isAlarmed = IsAlarmed();
        var isEnabled = IsEnabled();
        var isInitializing = IsInitializing();
        var isHomeDet = GetSensorValue(AxisSensorType.Home);
        var isNegativeLimitDet = GetSensorValue(AxisSensorType.NegativeLimit);
        var isPositiveLimitDet = GetSensorValue(AxisSensorType.PositiveLimit);

        var updated = false;
        lock (this)
        {
            updated |= UpdateCache(ref _commandPositionCache, commandPosition);
            updated |= UpdateCache(ref _actualPositionCache, actualPosition);
            updated |= UpdateCache(ref _isMovingCache, isMoving);
            updated |= UpdateCache(ref _isAlarmedCache, isAlarmed);
            updated |= UpdateCache(ref _isEnabledCache, isEnabled);
            updated |= UpdateCache(ref _isInitializingCache, isInitializing);
            updated |= UpdateCache(ref _isHomeDetCache, isHomeDet);
            updated |= UpdateCache(ref _isNegativeLimitDetCache, isNegativeLimitDet);
            updated |= UpdateCache(ref _isPositiveLimitDetCache, isPositiveLimitDet);
        }

        if (updated)
            SendDataToUi(Guid.Empty);
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemDataRead":
                SendDataToUi(message.Id);
                return true;
            case "_itemDataWrite":
            {
                if (message.DictPayload!.GetValueOrDefault("Enable") is bool enable)
                    Enable(enable);
                return true;
            }
            case "_initialize":
                Initialize();
                return true;
            case "_getStepJogSizes":
            {
                var sizes = StepJogSizes.Value.Items.ToList().ConvertAll(x => (double)x!).ToArray();
                message.Sender.Send(new Message(message, Actor, "_stepJogSizes", sizes));
                return true;
            }

            case "_jogStart":
            {
                Logger.Debug("Continuous Jog Start");
                var direction = (AxisDirection)message.DictPayload!["Direction"]!;
                var jogSpeed = (JogSpeed)message.DictPayload!["JogSpeed"]!;
                var speed = GetJogSpeed(jogSpeed);
                SetSpeed(speed);
                VelocityMove(direction);
                message.Sender.Send(new Message(message, Actor, "_jogStarted"));
                return true;
            }
            case "_jogStop":
            {
                Logger.Debug("Continuous Jog Stop");
                Stop();
                message.Sender.Send(new Message(message, Actor, "_jogStopped"));
                return true;
            }
        }

        return base.ProcessMessage(message);
    }

    public virtual void Enable(bool value)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.Enable(Channel, value);
        if (value)
            TimeManager.Sleep(EnableDelay.Value);
        if (IsEnabled() != value)
            throw new FatalSequenceError("Enabling axis has been failed.");
        RefreshCache();
    }

    public SpeedProfile GetJogSpeed(JogSpeed jogSpeed)
    {
        return (SpeedProfile)JogSpeed.ValueObject!;
    }

    public void Enable()
    {
        Enable(true);
    }

    public void Disable()
    {
        Enable(false);
    }

    public bool IsAlarmed()
    {
        return false;
    }

    public virtual bool IsEnabled()
    {
        return true; // TODO
    }

    public virtual bool IsInitializing()
    {
        return _initializing;
    }

    public bool IsNear(double position, double range)
    {
        return Math.Abs(GetPosition(PositionType.Command) - position) <= range;
    }

    public bool IsPosition(PositionComparisonType type, double position)
    {
        switch (type)
        {
            case PositionComparisonType.Greater:
                return GetPosition(PositionType.Command) > position;
            case PositionComparisonType.GreaterOrEqual:
                return GetPosition(PositionType.Command) >= position;
            case PositionComparisonType.Less:
                return GetPosition(PositionType.Command) < position;
            case PositionComparisonType.LessOrEqual:
                return GetPosition(PositionType.Command) <= position;
            default:
                throw new ValueError();
        }
    }

    public void WaitForPosition(PositionComparisonType type, double position)
    {
        while (true)
        {
            if (IsPosition(type, position))
                return;
            if (!IsMoving())
            {
                if (IsPosition(type, position))
                    return;
                throw new SequenceError("Couldn't meet the condition.");
            }

            timeManager.Sleep(1);
        }
    }

    public virtual bool IsMoving()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return false;
        }

        return MotionDevice.IsMoving(Channel);
    }

    public virtual void Move(double position)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        ValidateBeforeMove();
        MotionDevice.TrapezoidalMove(0, (int)position, 10000, 10000, 10000);
        MonitorMoving();
    }

    public void MoveAndWait(double position)
    {
        Move(position);
        Wait();
    }

    public void SetSpeed(IVariable speedProfileVariable)
    {
        SpeedProfile = (SpeedProfile)speedProfileVariable.ValueObject!;
    }

    public void SetSpeed(SpeedProfile speedProfile)
    {
        SpeedProfile = speedProfile;
    }

    public virtual void VelocityMove(AxisDirection direction)
    {
        // TODO
    }

    public virtual void Stop()
    {
        throw new NotImplementedException();
    }

    public virtual void SetPosition(
        double position,
        PositionType type = PositionType.CommandAndActual
    )
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }
        switch (type)
        {
            case PositionType.Command:
                MotionDevice.SetCommandPosition(position);
                break;
            case PositionType.Actual:
                MotionDevice.SetActualPosition(position);
                break;
            case PositionType.CommandAndActual:
                MotionDevice.SetCommandPosition(position);
                MotionDevice.SetActualPosition(position);
                break;
            case PositionType.Target:
                throw new ValueError();
            default:
                throw new ValueError();
        }
        RefreshCache();
    }

    public virtual void Wait()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.Wait(Channel);
        if (!IsMoving())
            return;
        Logger.Error(
            $"Monitoring task finished but axis is still moving. Fallback by spinning wait. ({ActorName}, {ItemPath})"
        );
        while (IsMoving()) // Fallback
            timeManager.Sleep(1);
    }

    public virtual double GetPosition(PositionType type)
    {
        return 0; // TODO
    }

    public virtual bool GetSensorValue(AxisSensorType type)
    {
        return type switch
        {
            AxisSensorType.Home => false,
            AxisSensorType.PositiveLimit => false,
            AxisSensorType.NegativeLimit => false,
            _ => throw new ValueError(),
        };
    }

    public virtual void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout)
    {
        var watch = timeManager.CreateWatch();
        while (GetSensorValue(type) != waitingValue)
        {
            if (watch.ElapsedMilliseconds > millisecondsTimeout)
                throw new TimeoutError();
            timeManager.Sleep(1);
        }
    }

    public void SetInitializeAction(Action initializeAction)
    {
        _initializeAction = initializeAction;
    }

    public void Initialize()
    {
        if (_initializeAction == null)
        {
            Logger.Error("The initialize action must be set before it can be used.");
            return;
        }

        _initializing = true;
        RefreshCache();

        _initializeAction();
        _initializing = false;
        RefreshCache();
    }

    protected virtual void MonitorMoving()
    {
        // Empty
    }

    private void SendDataToUi(Guid requestId)
    {
        Dict payload;
        lock (this)
        {
            payload = new Dict
            {
                ["CommandPosition"] = _commandPositionCache,
                ["ActualPosition"] = _actualPositionCache,
                ["IsMoving"] = _isMovingCache,
                ["IsAlarmed"] = _isAlarmedCache,
                ["IsEnabled"] = _isEnabledCache,
                ["IsInitializing"] = _isInitializingCache,
                ["IsHomeDet"] = _isHomeDetCache,
                ["IsNegativeLimitDet"] = _isNegativeLimitDetCache,
                ["IsPositiveLimitDet"] = _isPositiveLimitDetCache,
            };
        }

        Actor.Ui?.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemDataChanged", payload)
        );
    }

    private static bool UpdateCache<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        return true;
    }

    protected void ValidateBeforeMove()
    {
        if (SpeedProfile == null)
            throw new ValueError("You need to provide a SpeedProfile to move the axis.");
        if (SpeedProfile!.Velocity == 0)
            throw new ValueError("You must provide a speed greater than 0 to move the axis.");
        if (!IsMoving())
            return;
        Logger.Warn(
            $"Motion is still moving when it's trying to start move. ({ActorName}:{ItemPath})"
        );
        Stop();
        Wait();
    }

    #region Cache

    private double _commandPositionCache;
    private double _actualPositionCache;
    private bool _isAlarmedCache;
    private bool _isEnabledCache;
    private bool _isHomeDetCache;
    private bool _isInitializingCache;
    private bool _isMovingCache;
    private bool _isNegativeLimitDetCache;
    private bool _isPositiveLimitDetCache;

    #endregion
}
