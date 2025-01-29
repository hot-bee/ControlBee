using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class FakeAxis : Axis, IDisposable
{
    private const double Tolerance = 1e-6;
    private readonly IScenarioFlowTester _flowTester;
    private readonly ITimeManager _timeManager;
    private double _actualPosition;
    private double _commandPosition;
    private bool _homeSensor;
    private bool _isMoving;
    private bool _negativeLimitSensor;
    private bool _positiveLimitSensor;
    private readonly bool _skipWait;
    private double _targetPosition;

    public FakeAxis(ITimeManager timeManager, IScenarioFlowTester flowTester)
        : this(timeManager, flowTester, false) { }

    public FakeAxis(ITimeManager timeManager, IScenarioFlowTester flowTester, bool skipWait)
        : base(EmptyDeviceManager.Instance, timeManager)
    {
        _timeManager = timeManager;
        _flowTester = flowTester;
        _skipWait = skipWait;
        _timeManager.CurrentTimeChanged += TimeManagerOnCurrentTimeChanged;
    }

    public override bool IsMoving()
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

    public void Dispose()
    {
        _timeManager.CurrentTimeChanged -= TimeManagerOnCurrentTimeChanged;
    }

    public override void Move(double position)
    {
        ValidateBeforeMoving();
        _targetPosition = position;
        _isMoving = true;
        _flowTester.OnCheckpoint();
    }

    public override void VelocityMove(AxisDirection direction)
    {
        ValidateBeforeMoving();
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
        _flowTester.OnCheckpoint();
    }

    public override void Wait()
    {
        if (_skipWait)
        {
            SetPosition(GetPosition(PositionType.Target));
            _isMoving = false;
            return;
        }
        base.Wait();
    }

    public override void Stop()
    {
        if (!_isMoving)
            return;
        _targetPosition = _commandPosition;
        _isMoving = false;
        _flowTester.OnCheckpoint();
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
    }

    private void TimeManagerOnCurrentTimeChanged(object? sender, int elapsedMilliSeconds)
    {
        if (!_isMoving)
            return;
        var elapsedSeconds = elapsedMilliSeconds / 1000.0;
        var remainDistance = _targetPosition - _commandPosition;
        var movingDirection = double.Sign(remainDistance);
        var movingDistance = movingDirection * SpeedProfile!.Velocity * elapsedSeconds;
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
        if (_skipWait)
            return;
        base.WaitSensor(type, waitingValue, millisecondsTimeout);
    }
}
