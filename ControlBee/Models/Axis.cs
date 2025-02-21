using System.Reflection;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Axis(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IAxis
{
    private static readonly ILog Logger = LogManager.GetLogger(
        MethodBase.GetCurrentMethod()!.DeclaringType!
    );

    private Action? _initializeAction;

    private bool _initializing;

    private IVariable? _jogSpeedVariable;
    private Task? _task;

    protected SpeedProfile? SpeedProfile;

    public override void RefreshCache()
    {
        base.RefreshCache();
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
                    SetEnable(enable);
                return true;
            }
            case "_initialize":
                Initialize();
                return true;
        }

        return base.ProcessMessage(message);
    }

    public virtual void SetEnable(bool enable)
    {
        throw new NotImplementedException();
        RefreshCache();
    }

    public void SetJogSpeed(IVariable jogSpeedVariable)
    {
        _jogSpeedVariable = jogSpeedVariable;
    }

    public SpeedProfile? GetJogSpeed(JogSpeed jogSpeed)
    {
        if (_jogSpeedVariable == null)
        {
            Logger.Error($"Didn't set jog speed for this axis. ({ActorName}, {ItemPath})");
            return null;
        }

        return (SpeedProfile)_jogSpeedVariable.ValueObject!;
    }

    public void Enable()
    {
        SetEnable(true);
    }

    public void Disable()
    {
        SetEnable(false);
    }

    public bool IsAlarmed()
    {
        return false;
    }

    public virtual bool IsEnabled()
    {
        throw new NotImplementedException();
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
        return false;
    }

    public virtual void Move(double position)
    {
        ValidateBeforeMoving();
        // TODO
        MonitorMoving();
    }

    public void MoveAndWait(double position)
    {
        ValidateBeforeMoving();
        Move(position);
        MonitorMoving();
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
        throw new NotImplementedException();
    }

    public void Wait()
    {
        if (_task != null)
        {
            _task.Wait();
            _task = null;
        }

        if (IsMoving())
        {
            Logger.Error(
                $"Monitoring task finished but axis is still moving. ({ActorName}, {ItemPath})"
            );
            while (IsMoving())
                timeManager.Sleep(1);
        }
    }

    public virtual double GetPosition(PositionType type)
    {
        throw new NotImplementedException();
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

    protected void MonitorMoving()
    {
        _task = TimeManager.RunTask(() =>
        {
            var watch = timeManager.CreateWatch();
            while (IsMoving())
                timeManager.Sleep(1);
        });
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

    protected void ValidateBeforeMoving()
    {
        if (SpeedProfile == null)
            throw new ValueError("You need to provide a SpeedProfile to move the axis.");
        if (SpeedProfile!.Velocity == 0)
            throw new ValueError("You must provide a speed greater than 0 to move the axis.");
        if (IsMoving())
            Logger.Warn(
                $"Motion is still moving when it's trying to start move. ({ActorName}:{ItemPath})"
            );
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
