using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Sequences;
using ControlBee.Utils;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;
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
    protected bool _velocityMoving;

    protected SpeedProfile? CurrentSpeedProfile;

    public Variable<int> DisableDelay = new(VariableScope.Global, 200);
    public Variable<int> EnableDelay = new(VariableScope.Global, 200);
    public IDialog AxisAlarmError = new DialogPlaceholder();
    public IDialog HomeSensorTimeoutError = new DialogPlaceholder();

    public AxisDirection InitDirection = AxisDirection.Positive;

    public Variable<Position1D> InitPos = new(VariableScope.Global);

    public AxisSensorType InitSensorType;

    public Variable<SpeedProfile> InitSpeed = new(
        VariableScope.Global,
        new SpeedProfile
        {
            Velocity = 10.0,
            Accel = 100.0,
            Decel = 100.0,
            AccelJerkRatio = 0.75,
            DecelJerkRatio = 0.75
        }
    );

    public InitializeSequence InternalInitializeSequence;

    public bool IsJogReversed;

    public Variable<SpeedProfile> JogSpeed = new(
        VariableScope.Global,
        new SpeedProfile
        {
            Velocity = 10.0,
            Accel = 100.0,
            Decel = 100.0,
            AccelJerkRatio = 0.75,
            DecelJerkRatio = 0.75
        }
    );

    public Variable<Array1D<double>> JogSpeedLevelFactors = new(
        VariableScope.Global,
        new Array1D<double>([0.04, 0.2, 1.0])
    );

    public IDialog NegativeLimitSensorTimeoutError = new DialogPlaceholder();

    public Variable<SpeedProfile> NormalSpeed = new(
        VariableScope.Global,
        new SpeedProfile
        {
            Velocity = 10.0,
            Accel = 100.0,
            Decel = 100.0,
            AccelJerkRatio = 0.75,
            DecelJerkRatio = 0.75
        }
    );

    public IDialog PositiveLimitSensorTimeoutError = new DialogPlaceholder();

    public bool ResetEnableToClearPosition;

    public Variable<double> Resolution = new(VariableScope.Global, 1.0);

    public Variable<Array1D<double>> StepJogSizes = new(
        VariableScope.Global,
        new Array1D<double>([0.1, 0.5, 1.0])
    );

    public Axis(IDeviceManager deviceManager, ITimeManager timeManager)
        : base(deviceManager)
    {
        _timeManager = timeManager;
    }

    // ReSharper disable once SuspiciousTypeConversion.Global
    protected virtual IMotionDevice? MotionDevice => Device as IMotionDevice;
    public double ResolutionValue => Resolution.Value;

    public override void Init()
    {
        Actor.PositionAxesMap.Add(InitPos, [this]);
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (
            dataSource.GetValue(ActorName, ItemPath, nameof(InitSensorType))
            is string initSensorType
        )
            Enum.TryParse(initSensorType, out InitSensorType);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(InitDirection)) is string initDirection)
            Enum.TryParse(initDirection, out InitDirection);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(IsJogReversed)) is string isJogReversed)
            bool.TryParse(isJogReversed, out IsJogReversed);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(ResetEnableToClearPosition)) is string
            resetEnableToClearPosition)
            bool.TryParse(resetEnableToClearPosition, out ResetEnableToClearPosition);

        InternalInitializeSequence = new InitializeSequence(
            this,
            InitSpeed,
            InitPos,
            InitSensorType,
            InitDirection
        );
        _initializeAction = () => { InternalInitializeSequence.Run(); };
    }

    public override void RefreshCache(bool alwaysUpdate = false)
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
                if (IsJogReversed) direction = (AxisDirection)((int)direction * -1);
                switch (type)
                {
                    case "Continuous":
                    {
                        Logger.Debug("Continuous Jog Start");

                        if (DictPath.Start(message.DictPayload)["JogSpeed"].Value is JogSpeedLevel jogSpeed)
                        {
                            var speed = GetJogSpeed(jogSpeed);
                            SetSpeed(speed);
                        }
                        else if (DictPath.Start(message.DictPayload)["JogSpeedRatio"].Value is double jogSpeedRatio)
                        {
                            var speed = (SpeedProfile)GetJogSpeed(JogSpeedLevel.Fast).Clone();
                            speed.Velocity *= jogSpeedRatio;
                            SetSpeed(speed);
                        }
                        else
                        {
                            throw new ValueError();
                        }

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
                        var customStepSize = (double?)message.DictPayload!.GetValueOrDefault("CustomStepSize");
                        double step;
                        if(jogStep == JogStep.Custom)
                            step = customStepSize!.Value * (int)direction;
                        else
                            step = StepJogSizes.Value[(int)jogStep] * (int)direction;
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
        Stopwatch sw = new();
        sw.Restart();
        while (IsEnabled() != value)
        {
            if (sw.ElapsedMilliseconds > 5000)
                throw new TimeoutError($"Failed to enable or disable axis. ({Channel})");
            Thread.Sleep(1);
        }

        TimeManager.Sleep(value ? EnableDelay.Value : DisableDelay.Value);
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

    public SpeedProfile GetInitSpeed()
    {
        return (SpeedProfile)InitSpeed.ValueObject!;
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
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return false;
        }

        return MotionDevice.IsAlarmed(Channel);
    }

    public void ClearAlarm()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.ClearAlarm(Channel);
        Stopwatch sw = new();
        sw.Restart();
        while (IsAlarmed())
        {
            if (sw.ElapsedMilliseconds > 5000)
                throw new TimeoutError($"Failed to clear alarm of axis. ({Channel})");
            Thread.Sleep(1);
        }
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

    public void OnBeforeInitialize()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.OnBeforeInitialize(Channel);
    }

    public bool IsNear(double position, double range)
    {
        return Math.Abs(GetPosition(PositionType.Command) - position) <= range;
    }

    public bool WaitNear(double position, double range)
    {
        while (true)
        {
            if (IsNear(position, range))
                return true;
            if (!IsMoving())
            {
                if (IsNear(position, range))
                    return true;
                return false;
            }

            _timeManager.Sleep(1);
        }
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

    public bool WaitForPosition(PositionComparisonType type, double position)
    {
        while (true)
        {
            if (IsPosition(type, position))
                return true;
            if (!IsMoving())
            {
                if (IsPosition(type, position))
                    return true;
                return false;
            }

            _timeManager.Sleep(1);
        }
    }

    public bool IsFar(double position, double range)
    {
        return Math.Abs(GetPosition(PositionType.Command) - position) > range;
    }

    public bool WaitFar(double position, double range)
    {
        while (true)
        {
            if (IsFar(position, range))
                return true;
            if (!IsMoving())
            {
                if (IsFar(position, range))
                    return true;
                return false;
            }

            _timeManager.Sleep(1);
        }
    }

    public virtual bool IsMoving(PositionType type = PositionType.CommandAndActual)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return false;
        }

        return MotionDevice.IsMoving(Channel, type);
    }

    public virtual void SearchZPhase(double distance)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.SearchZPhase(
            Channel,
            InitSpeed.Value.Velocity * Resolution.Value * (int)InitDirection,
            Math.Abs(InitSpeed.Value.Accel * Resolution.Value),
            distance
        );
    }

    public bool IsVelocityMoving()
    {
        return _velocityMoving;
    }

    public InitializeSequence InitializeSequence => InternalInitializeSequence;

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

        try
        {
            ValidateBeforeMove(@override);
            MotionDevice.JerkRatioSCurveMove(
                Channel,
                position * Resolution.Value,
                Math.Abs(CurrentSpeedProfile.Velocity * Resolution.Value),
                Math.Abs(CurrentSpeedProfile.Accel * Resolution.Value),
                Math.Abs(CurrentSpeedProfile.Decel * Resolution.Value),
                CurrentSpeedProfile.AccelJerkRatio,
                CurrentSpeedProfile.DecelJerkRatio
            );
            MonitorMoving();
            _velocityMoving = false;
        }
        catch (AxisAlarmError)
        {
            AxisAlarmError.Show();
            throw;
        }
    }

    public void RelativeMove(double distance)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            ValidateBeforeMove(false);
            MotionDevice.JerkRatioSCurveRelativeMove(
                Channel,
                distance * Resolution.Value,
                Math.Abs(CurrentSpeedProfile.Velocity * Resolution.Value),
                Math.Abs(CurrentSpeedProfile.Accel * Resolution.Value),
                Math.Abs(CurrentSpeedProfile.Decel * Resolution.Value),
                CurrentSpeedProfile.AccelJerkRatio,
                CurrentSpeedProfile.DecelJerkRatio
            );
            MonitorMoving();
        }
        catch (NotImplementedException)
        {
            var position = GetPosition(PositionType.Command) + distance;
            Move(position);
        }
    }

    public void MoveAndWait(double position, PositionType type = PositionType.CommandAndActual)
    {
        Move(position);
        Wait(type);
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
        VelocityMove(direction, false);
    }

    public virtual void VelocityMove(AxisDirection direction, bool @override)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        ValidateBeforeMove(@override);
        var velocity = CurrentSpeedProfile.Velocity * (double)direction;
        MotionDevice.VelocityMove(
            Channel,
            velocity * Resolution.Value,
            Math.Abs(CurrentSpeedProfile.Accel * Resolution.Value),
            Math.Abs(CurrentSpeedProfile.Decel * Resolution.Value),
            CurrentSpeedProfile.AccelJerkRatio,
            CurrentSpeedProfile.DecelJerkRatio
        );
        _velocityMoving = true;
    }

    public virtual void Stop()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.Stop(Channel);
        _velocityMoving = false;
    }

    public virtual void EStop()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.EStop(Channel);
        _velocityMoving = false;
    }

    public void ClearPosition(PositionType type = PositionType.CommandAndActual)
    {
        if (ResetEnableToClearPosition)
        {
            Logger.Info($"Reset Enable. ({ActorName}, {ItemPath})");
            Disable();
            Enable();
            return;
        }

        SetPosition(0.0, type);
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
                MotionDevice.SetCommandPosition(Channel, position * Resolution.Value);
                break;
            case PositionType.Actual:
                MotionDevice.SetActualPosition(Channel, position * Resolution.Value);
                break;
            case PositionType.CommandAndActual:
                MotionDevice.SetCommandPosition(Channel, position * Resolution.Value);
                MotionDevice.SetActualPosition(Channel, position * Resolution.Value);
                break;
            case PositionType.Target:
                throw new ValueError();
            default:
                throw new ValueError();
        }

        RefreshCache();
    }

    public virtual void SetTorque(double torque)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.SetTorque(Channel, torque);
    }

    public virtual void Wait(PositionType type = PositionType.CommandAndActual)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        MotionDevice.Wait(Channel, type);
        if (!IsMoving(type))
            return;
        Logger.Error(
            $"Monitoring task finished but axis is still moving. Fallback by spinning wait. ({ActorName}, {ItemPath})"
        );
        while (IsMoving(type)) // Fallback
            _timeManager.Sleep(1);
        if (IsAlarmed())
        {
            AxisAlarmError.Show();
            throw new AxisAlarmError();
        }
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
                return MotionDevice.GetCommandPosition(Channel) / Resolution.Value;
            case PositionType.Actual:
                return MotionDevice.GetActualPosition(Channel) / Resolution.Value;
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
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return 0;
        }

        switch (type)
        {
            case VelocityType.Command:
                return MotionDevice.GetCommandVelocity(Channel) / Resolution.Value;
            case VelocityType.Actual:
                return MotionDevice.GetActualVelocity(Channel) / Resolution.Value;
            default:
                throw new ValueError();
        }
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
            if (watch.ElapsedMilliseconds > millisecondsTimeout || millisecondsTimeout == 0)
            {
                switch (type)
                {
                    case AxisSensorType.Home:
                        HomeSensorTimeoutError.Show();
                        break;
                    case AxisSensorType.PositiveLimit:
                        PositiveLimitSensorTimeoutError.Show();
                        break;
                    case AxisSensorType.NegativeLimit:
                        NegativeLimitSensorTimeoutError.Show();
                        break;
                }

                throw new TimeoutError();
            }

            _timeManager.Sleep(1);
        }
    }

    public void SetInitializeAction(Action initializeAction)
    {
        _initializeAction = initializeAction;
    }

    public void Initialize()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        _initializing = true;
        RefreshCache();

        _initializeAction();
        _initializing = false;
        RefreshCache();
    }

    public void BuiltinInitialize()
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        Logger.Info($"Start built-in initialize. ({ActorName}, {ItemPath})");
        MotionDevice.BuiltinInitialize(Channel);
    }

    public void SpecialCommand(Dict data)
    {
        if (MotionDevice == null)
        {
            Logger.Error($"MotionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        Logger.Info($"Start special command. ({ActorName}, {ItemPath})");
        MotionDevice.SpecialCommand(Channel, data);
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
        if (IsAlarmed())
        {
            AxisAlarmError.Show();
            throw new AxisAlarmError();
        }
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