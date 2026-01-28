using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Exceptions;
using log4net;

namespace ControlBee.Models;

public class FakeAxis : Axis, IDisposable
{
    private const double Tolerance = 1e-6;
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Axis));
    private readonly IScenarioFlowTester _flowTester;
    private readonly bool _skipWaitSensor;
    private readonly ITimeManager _timeManager;
    private double _actualPosition;
    private double _commandPosition;
    private bool _homeSensor;
    private bool _isEnabled;
    private bool _isMoving;

    private Task? _movingTask;
    private bool _negativeLimitSensor;
    private bool _positiveLimitSensor;
    private double _targetPosition;

    public FakeAxis(
        IDeviceManager deviceManager,
        ITimeManager timeManager,
        IScenarioFlowTester flowTester,
        IInitializeSequenceFactory initializeSequenceFactory
    )
        : this(deviceManager, timeManager, flowTester, false, initializeSequenceFactory) { }

    public FakeAxis(
        IDeviceManager deviceManager,
        ITimeManager timeManager,
        IScenarioFlowTester flowTester,
        bool skipWaitSensor,
        IInitializeSequenceFactory initializeSequenceFactory
    )
        : base(deviceManager, timeManager, initializeSequenceFactory)
    {
        _timeManager = timeManager;
        _flowTester = flowTester;
        _skipWaitSensor = skipWaitSensor;
        _timeManager.CurrentTimeChanged += TimeManagerOnCurrentTimeChanged;
    }

    public bool IsMovingMonitored => _movingTask != null;

    public void Dispose()
    {
        _timeManager.CurrentTimeChanged -= TimeManagerOnCurrentTimeChanged;
    }

    public override bool IsMoving(PositionType type = PositionType.CommandAndActual)
    {
        return _isMoving;
    }

    public override bool GetSensorValue(AxisSensorType type)
    {
        return type switch
        {
            AxisSensorType.Home => _homeSensor,
            AxisSensorType.PositiveLimit => _positiveLimitSensor,
            AxisSensorType.NegativeLimit => _negativeLimitSensor,
            _ => throw new ValueError(),
        };
    }

    public override bool GetSensorValueOrTrue(AxisSensorType type)
    {
        if (_skipWaitSensor)
            return true;
        return base.GetSensorValueOrTrue(type);
    }

    public override bool GetSensorValueOrFalse(AxisSensorType type)
    {
        if (_skipWaitSensor)
            return false;
        return base.GetSensorValueOrFalse(type);
    }

    public override void Move(double position, bool @override)
    {
        ValidateBeforeMove(@override);
        _targetPosition = position;
        _isMoving = true;
        _velocityMoving = false;
        _flowTester.OnCheckpoint();
        RefreshCache();
        MonitorMoving(@override);
    }

    public override void RelativeMove(double distance)
    {
        var commandPos = GetPosition(PositionType.Command);
        Move(commandPos + distance);
    }

    public override void VelocityMove(AxisDirection direction, bool @override)
    {
        ValidateBeforeMove(false);
        switch (direction)
        {
            case AxisDirection.Positive:
                _targetPosition = double.PositiveInfinity;
                break;
            case AxisDirection.Negative:
                _targetPosition = double.NegativeInfinity;
                break;
            default:
                throw new ValueError();
        }

        _isMoving = true;
        _velocityMoving = true;
        _flowTester.OnCheckpoint();
        RefreshCache();
        MonitorMoving();
    }

    public override void Stop()
    {
        if (!_isMoving)
            return;
        _targetPosition = _commandPosition;
        _isMoving = false;
        _velocityMoving = false;
        _flowTester.OnCheckpoint();
        RefreshCache();
        Wait();
    }

    public override void EStop()
    {
        Stop();
    }

    public override double GetPosition(PositionType type)
    {
        switch (type)
        {
            case PositionType.Command:
                return _commandPosition;
            case PositionType.Actual:
                return _actualPosition;
            case PositionType.CommandAndActual:
                throw new ValueError();
            case PositionType.Target:
                return _targetPosition;
            default:
                throw new ValueError();
        }
    }

    public override void SetPosition(
        double position,
        PositionType type = PositionType.CommandAndActual
    )
    {
        switch (type)
        {
            case PositionType.Command:
                _commandPosition = position;
                break;
            case PositionType.Actual:
                _actualPosition = position;
                break;
            case PositionType.CommandAndActual:
                _commandPosition = position;
                _actualPosition = position;
                break;
            case PositionType.Target:
                throw new ValueError();
            default:
                throw new ValueError();
        }

        RefreshCache();
    }

    private void TimeManagerOnCurrentTimeChanged(object? sender, int elapsedMilliSeconds)
    {
        if (!_isMoving)
            return;
        var elapsedSeconds = elapsedMilliSeconds / 1000.0;
        var remainDistance = _targetPosition - _commandPosition;
        var movingDirection = double.Sign(remainDistance);
        var movingDistance = movingDirection * CurrentSpeedProfile!.Velocity * elapsedSeconds;
        if (Math.Abs(remainDistance) < Math.Abs(movingDistance))
            movingDistance = remainDistance;
        _commandPosition += movingDistance;
        _actualPosition = _commandPosition;
        if (Math.Abs(_commandPosition - _targetPosition) < Tolerance)
        {
            _commandPosition = _actualPosition = _targetPosition;
            _isMoving = false;
        }

        _flowTester.OnCheckpoint();
    }

    public void SetSensorValue(AxisSensorType type, bool value)
    {
        switch (type)
        {
            case AxisSensorType.Home:
                _homeSensor = value;
                break;
            case AxisSensorType.PositiveLimit:
                _positiveLimitSensor = value;
                break;
            case AxisSensorType.NegativeLimit:
                _negativeLimitSensor = value;
                break;
            default:
                throw new ValueError();
        }

        _flowTester.OnCheckpoint();
    }

    public override void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout)
    {
        if (_skipWaitSensor)
            return;
        base.WaitSensor(type, waitingValue, millisecondsTimeout);
    }

    public override bool IsAlarmed()
    {
        return false;
    }

    public override bool IsEnabled()
    {
        return _isEnabled;
    }

    public override void Enable(bool value)
    {
        _isEnabled = value;
        RefreshCache();
    }

    public override void RefreshCache(bool alwaysUpdate = false)
    {
        RefreshCacheImpl();
    }

    protected override void MonitorMoving(bool @override = false)
    {
        if (_movingTask != null)
        {
            if (@override)
                return;
            Logger.Error("`_movingTask` should be null here.");
            _movingTask.Wait();
            _movingTask = null;
        }

        _movingTask = TimeManager.RunTask(() =>
        {
            while (IsMoving())
                _timeManager.Sleep(1);
        });
    }

    public override void Wait(PositionType type = PositionType.CommandAndActual)
    {
        if (_movingTask != null)
            try
            {
                _movingTask.Wait();
                _movingTask = null;
            }
            catch (AggregateException exception)
            {
                throw exception.InnerExceptions[0];
            }

        if (!IsMoving(type))
            return;
        Logger.Error(
            $"Monitoring task finished but axis is still moving. Fallback by spinning wait. ({ActorName}, {ItemPath})"
        );
        while (IsMoving(type)) // Fallback
            _timeManager.Sleep(1);
    }

    public override void SearchZPhase(double distance)
    {
        // Empty
    }

    public override void SetTorque(double torque)
    {
        // Empty
    }
}
