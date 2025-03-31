using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Sequences;
using ControlBee.Utils;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Axis : DeviceChannel, IAxis
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Axis));

    private Action _initializeAction;
    private bool _initializing;

    protected SpeedProfile? CurrentSpeedProfile;

    public Variable<int> EnableDelay = new(VariableScope.Global, 200);

    public Variable<Position1D> InitPos = new(VariableScope.Global);

    public Variable<SpeedProfile> InitSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    public AxisDirection InitDirection;

    public InitializeSequence InitializeSequence;

    public AxisSensorType InitSensorType;

    public Variable<SpeedProfile> JogSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    public Variable<Array1D<double>> JogSpeedLevelFactors = new(
        VariableScope.Global,
        new Array1D<double>([0.04, 0.2, 1.0])
    );

    public Variable<SpeedProfile> NormalSpeed = new(
        VariableScope.Global,
        new SpeedProfile { Velocity = 10.0 }
    );

    public Variable<Array1D<double>> StepJogSizes = new(
        VariableScope.Global,
        new Array1D<double>([0.01, 0.1, 1.0])
    );

    public Axis(IDeviceManager deviceManager, ITimeManager timeManager)
        : base(deviceManager)
    {
        _timeManager = timeManager;
    }

    // ReSharper disable once SuspiciousTypeConversion.Global
    protected virtual IMotionDevice? MotionDevice => Device as IMotionDevice;

    public override void Init()
    {
        Actor.PositionAxesMap.Add(InitPos, [this]);
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(InitSensorType)) is string initSensorType)
            Enum.TryParse(initSensorType, out InitSensorType);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(InitDirection)) is string initDirection)
            Enum.TryParse(initDirection, out InitDirection);

        InitializeSequence = new InitializeSequence(this, InitSpeed, InitPos, InitSensorType, InitDirection);
        _initializeAction = () => { InitializeSequence.Run(); };
    }

    public override void RefreshCache()
    {
        base.RefreshCache();
        if (MotionDevice == null)
            return;
        RefreshCacheImpl();
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
                var type = (string)message.DictPayload!["Type"]!;
                var direction = (AxisDirection)message.DictPayload!["Direction"]!;
                switch (type)
                {
                    case "Continuous":
                    {
                        Logger.Debug("Continuous Jog Start");
                        var jogSpeed = (JogSpeedLevel)message.DictPayload!["JogSpeed"]!;
                        var speed = GetJogSpeed(jogSpeed);
                        SetSpeed(speed);
                        VelocityMove(direction);
                        message.Sender.Send(new Message(message, Actor, "_jogStarted"));
                        break;
                    }
                    case "Step":
                    {
                        Logger.Debug("Step Jog Start");
                        if (IsMoving())
                            Logger.Warn("Cancel jog. It's already moving now.");
                        var jogStep = (JogStep)message.DictPayload!["JogStep"]!;
                        var step = StepJogSizes.Value[(int)jogStep] * (int)direction;
                        var speed = GetJogSpeed(JogSpeedLevel.Medium);
                        SetSpeed(speed);
                        RelativeMove(step);
                        message.Sender.Send(new Message(message, Actor, "_jogStarted"));
                        break;
                    }
                }

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
        TimeManager.Sleep(1000);
        Stopwatch sw = new();
        sw.Restart();
        while (IsEnabled() != value)
        {
            if (sw.ElapsedMilliseconds > 5000) throw new TimeoutError($"Failed to enable or disable axis. ({Channel})");
            Thread.Sleep(1);
        }

        if (value)
            TimeManager.Sleep(EnableDelay.Value);
        RefreshCache();
    }

    public SpeedProfile GetJogSpeed(JogSpeedLevel jogSpeedLevel)
    {
        var jogSpeed = (SpeedProfile)JogSpeed.ValueObject!;
        jogSpeed = (SpeedProfile)jogSpeed.Clone();
        switch (jogSpeedLevel)
        {
            case JogSpeedLevel.Slow:
                jogSpeed.Velocity *= JogSpeedLevelFactors.Value[0];
                break;
            case JogSpeedLevel.Medium:
                jogSpeed.Velocity *= JogSpeedLevelFactors.Value[1];
                break;
            case JogSpeedLevel.Fast:
                jogSpeed.Velocity *= JogSpeedLevelFactors.Value[2];
                break;
        }

        return jogSpeed;
    }

    public SpeedProfile GetNormalSpeed()
    {
        return (SpeedProfile)NormalSpeed.ValueObject!;
    }

    public Position1D GetInitPos()
    {
        return (Position1D)InitPos.ValueObject!;
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
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return false;
        }

        return MotionDevice.IsEnabled(Channel);
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

            _timeManager.Sleep(1);
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

    public void Move(double position)
    {
        Move(position, false);
    }

    public virtual void Move(double position, bool @override)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        ValidateBeforeMove(@override);
        MotionDevice.JerkRatioSCurveMove(Channel, position, CurrentSpeedProfile.Velocity, CurrentSpeedProfile.Accel,
            CurrentSpeedProfile.Decel, CurrentSpeedProfile.AccelJerkRatio, CurrentSpeedProfile.DecelJerkRatio);
        MonitorMoving();
    }

    public void RelativeMove(double distance)
    {
        var position = GetPosition(PositionType.Command) + distance;
        Move(position);
    }

    public void MoveAndWait(double position)
    {
        Move(position);
        Wait();
    }

    public void RelativeMoveAndWait(double distance)
    {
        RelativeMove(distance);
        Wait();
    }

    public void SetSpeed(IVariable speedProfileVariable)
    {
        CurrentSpeedProfile = (SpeedProfile)speedProfileVariable.ValueObject!;
    }

    public void SetSpeed(SpeedProfile speedProfile)
    {
        CurrentSpeedProfile = speedProfile;
    }

    public virtual void VelocityMove(AxisDirection direction)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        ValidateBeforeMove(false);
        var velocity = CurrentSpeedProfile.Velocity * (double)direction;
        MotionDevice.VelocityMove(Channel, velocity, CurrentSpeedProfile.Accel,
            CurrentSpeedProfile.Decel, CurrentSpeedProfile.AccelJerkRatio, CurrentSpeedProfile.DecelJerkRatio);
    }

    public virtual void Stop()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.Stop(Channel);
    }

    public void EStop()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.EStop(Channel);
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
                MotionDevice.SetCommandPosition(Channel, position);
                break;
            case PositionType.Actual:
                MotionDevice.SetActualPosition(Channel, position);
                break;
            case PositionType.CommandAndActual:
                MotionDevice.SetCommandPosition(Channel, position);
                MotionDevice.SetActualPosition(Channel, position);
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
            _timeManager.Sleep(1);
    }

    public virtual double GetPosition(PositionType type)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return 0;
        }

        switch (type)
        {
            case PositionType.Command:
                return MotionDevice.GetCommandPosition(Channel);
            case PositionType.Actual:
                return MotionDevice.GetActualPosition(Channel);
            case PositionType.CommandAndActual:
                throw new ValueError();
            case PositionType.Target:
                throw new NotImplementedException();
            default:
                throw new ValueError();
        }
    }

    public virtual double GetVelocity(VelocityType type)
    {
        return 0;
    }

    public virtual bool GetSensorValue(AxisSensorType type)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return false;
        }

        return type switch
        {
            AxisSensorType.Home => MotionDevice.GetHomeSensor(Channel),
            AxisSensorType.PositiveLimit => MotionDevice.GetPositiveLimitSensor(Channel),
            AxisSensorType.NegativeLimit => MotionDevice.GetNegativeLimitSensor(Channel),
            _ => throw new ValueError()
        };
    }

    public virtual void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout)
    {
        var watch = _timeManager.CreateWatch();
        while (GetSensorValue(type) != waitingValue)
        {
            if (watch.ElapsedMilliseconds > millisecondsTimeout)
                throw new TimeoutError();
            _timeManager.Sleep(1);
        }
    }

    public void SetInitializeAction(Action initializeAction)
    {
        _initializeAction = initializeAction;
    }

    public void Initialize()
    {
        _initializing = true;
        RefreshCache();

        _initializeAction();
        _initializing = false;
        RefreshCache();
    }

    protected void RefreshCacheImpl()
    {
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

    protected virtual void MonitorMoving(bool @override = false)
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
                ["IsPositiveLimitDet"] = _isPositiveLimitDetCache
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

    protected void ValidateBeforeMove(bool @override)
    {
        if (CurrentSpeedProfile == null)
            throw new ValueError("You need to provide a SpeedProfile to move the axis.");
        if (CurrentSpeedProfile!.Velocity == 0)
            throw new ValueError("You must provide a speed greater than 0 to move the axis.");
        if (!@override)
        {
            if (!IsMoving())
                return;
            Logger.Warn(
                $"Motion is still moving when it's trying to start move. ({ActorName}:{ItemPath})"
            );
            Stop();
            Wait();
        }
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
    private readonly ITimeManager _timeManager;

    #endregion
}