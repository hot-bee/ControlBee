using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class Axis(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IAxis
{
    private Action? _initializeAction;
    protected SpeedProfile? SpeedProfile;

    public virtual bool IsMoving()
    {
        return false;
    }

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
            throw new PlatformException("The initialize action must be set before it can be used.");
        _initializeAction();
    }

    public override void UpdateSubItem() { }

    public override void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        // TODO
    }

    protected void ValidateBeforeMoving()
    {
        if (SpeedProfile == null)
            throw new ValueError("You need to provide a SpeedProfile to move the axis.");
        if (SpeedProfile!.Velocity == 0)
            throw new ValueError("You must provide a speed greater than 0 to move the axis.");
    }
}
