using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class Axis : IAxis
{
    protected SpeedProfile? SpeedProfile;

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
}
