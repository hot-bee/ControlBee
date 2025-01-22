using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;

namespace ControlBee.Sequences;

public class InitializeSequence(IAxis axis, SpeedProfile homingSpeed, Position1D homePosition)
    : ActorItem
{
    public Alert SensorEntryTimeout = new();
    public Alert SensorExitTimeout = new();
    public Alert SensorReentryTimeout = new();

    public InitializeSequence(
        IAxis axis,
        Variable<SpeedProfile> homingSpeed,
        Variable<Position1D> homePosition
    )
        : this(axis, homingSpeed.Value, homePosition.Value) { }

    public void Run()
    {
        try
        {
            axis.SetSpeed(homingSpeed);
            axis.VelocityMove(AxisDirection.Negative);
            axis.WaitSensor(AxisSensorType.Home, true, 5000);
        }
        catch (TimeoutError)
        {
            SensorEntryTimeout.Trigger();
            throw new SequenceError();
        }
        finally
        {
            axis.Stop();
        }

        try
        {
            var halfHomingSpeed = (SpeedProfile)homingSpeed.Clone();
            halfHomingSpeed.Velocity /= 10;
            axis.SetSpeed(halfHomingSpeed);
            axis.VelocityMove(AxisDirection.Positive);
            axis.WaitSensor(AxisSensorType.Home, false, 5000);
        }
        catch (TimeoutError)
        {
            SensorExitTimeout.Trigger();
            throw new SequenceError();
        }
        finally
        {
            axis.Stop();
        }

        try
        {
            axis.VelocityMove(AxisDirection.Negative);
            axis.WaitSensor(AxisSensorType.Home, true, 5000);
        }
        catch (TimeoutError)
        {
            SensorReentryTimeout.Trigger();
            throw new SequenceError();
        }
        finally
        {
            axis.Stop();
        }

        axis.SetPosition(0.0);
        axis.SetSpeed(homingSpeed);
        homePosition.MoveAndWait();
    }

    public override void ProcessMessage(ActorItemMessage message) { }

    public override void UpdateSubItem() { }

    public override void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        // TODO
    }
}
