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
        axis.SetSpeed(homingSpeed);
        axis.VelocityMove(AxisDirection.Negative);

        var watch = TimeManager.CreateWatch();
        while (true)
        {
            if (watch.ElapsedSeconds >= 5.0)
            {
                SensorEntryTimeout.Trigger();
                throw new SequenceError();
            }

            if (axis.HomeSensor)
            {
                axis.Stop();
                break;
            }

            TimeManager.Sleep(1);
        }

        var halfHomingSpeed = (SpeedProfile)homingSpeed.Clone();
        halfHomingSpeed.Velocity /= 10;
        axis.SetSpeed(halfHomingSpeed);
        axis.VelocityMove(AxisDirection.Positive);
        watch.Restart();
        while (true)
        {
            if (watch.ElapsedSeconds >= 5.0)
            {
                SensorExitTimeout.Trigger();
                throw new SequenceError();
            }

            if (!axis.HomeSensor)
            {
                axis.Stop();
                break;
            }

            TimeManager.Sleep(1);
        }

        axis.VelocityMove(AxisDirection.Negative);
        watch.Restart();
        while (true)
        {
            if (watch.ElapsedSeconds >= 5.0)
            {
                SensorReentryTimeout.Trigger();
                throw new SequenceError();
            }

            if (axis.HomeSensor)
            {
                axis.Stop();
                break;
            }

            TimeManager.Sleep(1);
        }

        axis.SetPosition(0.0);
        axis.SetSpeed(homingSpeed);
        homePosition.MoveAndWait();
    }

    public override void ProcessMessage(Message message) { }

    public override void UpdateSubItem() { }
}
