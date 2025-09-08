using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Sequences;

public class InitializeSequence(
    IAxis axis,
    Variable<SpeedProfile> initSpeed,
    Variable<Position1D> homePosition,
    AxisSensorType sensorType,
    AxisDirection direction)
    : ActorItem,
        IInitializeSequence
{
    public Variable<int> DelayBeforeClearPosition = new(VariableScope.Global, 0);
    public IDialog SensorEntryTimeout = new DialogPlaceholder();
    public IDialog SensorExitTimeout = new DialogPlaceholder();
    public IDialog SensorReentryTimeout = new DialogPlaceholder();

    public void Run()
    {
        axis.ClearAlarm();
        axis.Enable(false);
        axis.Enable(true);
        axis.OnBeforeInitialize();

        switch (sensorType)
        {
            case AxisSensorType.Home:
            case AxisSensorType.NegativeLimit:
            case AxisSensorType.PositiveLimit:
                RunNormal();
                break;
            case AxisSensorType.ZPhase:
                RunZPhase();
                break;
            case AxisSensorType.Builtin:
                axis.BuiltinInitialize();
                break;
            default:
                throw new ValueError();
        }

        Thread.Sleep(DelayBeforeClearPosition.Value);
        axis.ClearPosition();
        axis.SetSpeed(axis.GetJogSpeed(JogSpeedLevel.Fast));
        homePosition.Value.MoveAndWait();
    }

    public void RunZPhase()
    {
        axis.SearchZPhase(0);
    }

    public void RunNormal()
    {
        try
        {
            axis.SetSpeed(initSpeed);
            if (axis.GetSensorValue(sensorType) != true) axis.VelocityMove(direction);
            axis.WaitSensor(sensorType, true, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorEntryTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            axis.EStop();
            axis.Wait();
        }

        try
        {
            var halfHomingSpeed = (SpeedProfile)initSpeed.Value.Clone();
            halfHomingSpeed.Velocity /= 10;
            axis.SetSpeed(halfHomingSpeed);
            axis.VelocityMove((AxisDirection)((int)direction * -1));
            axis.WaitSensor(sensorType, false, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorExitTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            axis.EStop();
            axis.Wait();
        }

        try
        {
            axis.VelocityMove(direction);
            axis.WaitSensor(sensorType, true, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorReentryTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            axis.EStop();
            axis.Wait();
        }
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        // TODO
    }
}