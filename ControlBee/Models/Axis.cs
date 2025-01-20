using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class Axis(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IAxis
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
        ValidateBeforeMoving();
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

    protected void ValidateBeforeMoving()
    {
        if (SpeedProfile == null)
            throw new ValueError("You need to provide a SpeedProfile to move the axis.");
        if (SpeedProfile!.Velocity == 0)
            throw new ValueError("You must provide a speed greater than 0 to move the axis.");
    }

    public override void ProcessMessage(Message message) { }

    public override void UpdateSubItem() { }
}
