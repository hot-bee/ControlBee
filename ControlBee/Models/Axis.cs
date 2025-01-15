using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class Axis : IAxis
{
    private readonly ITimeManager _timeManager;
    protected bool EmulationMode;
    protected SpeedProfile? SpeedProfile;

    public Axis(ITimeManager timeManager)
    {
        _timeManager = timeManager;
    }

    public virtual bool HomeSensor { get; } = false;
    public virtual bool PositiveLimitSensor { get; } = false;
    public virtual bool NegativeLimitSensor { get; } = false;
    public virtual bool IsMoving { get; } = false;

    public virtual void Move(double position)
    {
        // TODO
    }

    public virtual void MoveAndWait(double position)
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
        throw new NotImplementedException();
    }

    public virtual void Wait()
    {
        throw new NotImplementedException();
    }

    public virtual double GetPosition(PositionType type)
    {
        throw new NotImplementedException();
    }

    public bool GetSensorValue(AxisSensorType type)
    {
        switch (type)
        {
            case AxisSensorType.Home:
                return HomeSensor;
            case AxisSensorType.PositiveLimit:
                return PositiveLimitSensor;
            case AxisSensorType.NegativeLimit:
                return NegativeLimitSensor;
            default:
                throw new ValueError();
        }
    }

    public void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout)
    {
        if (EmulationMode)
            return;
        var watch = _timeManager.CreateWatch();
        while (GetSensorValue(type) != waitingValue)
        {
            if (watch.ElapsedMilliseconds > millisecondsTimeout)
                throw new TimeoutError();
            _timeManager.Sleep(1);
        }
    }
}
