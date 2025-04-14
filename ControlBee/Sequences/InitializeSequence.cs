using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Sequences;

public class InitializeSequence : ActorItem,
    IInitializeSequence
{
    private readonly IAxis _axis;
    private readonly AxisDirection _direction;
    private readonly Variable<Position1D> _homePosition;
    private readonly Variable<SpeedProfile> _initSpeed;
    private readonly AxisSensorType _sensorType;
    public IDialog SensorEntryTimeout = new DialogPlaceholder();
    public IDialog SensorExitTimeout = new DialogPlaceholder();
    public IDialog SensorReentryTimeout = new DialogPlaceholder();

    public InitializeSequence(
        IAxis axis,
        Variable<SpeedProfile> initSpeed,
        Variable<Position1D> homePosition,
        AxisSensorType sensorType,
        AxisDirection direction
    )
    {
        _axis = axis;
        _initSpeed = initSpeed;
        _homePosition = homePosition;
        _sensorType = sensorType;
        _direction = direction;
    }

    public void Run()
    {
        _axis.Enable(true);
        switch (_sensorType)
        {
            case AxisSensorType.Home:
            case AxisSensorType.NegativeLimit:
            case AxisSensorType.PositiveLimit:
                RunNormal();
                break;
            case AxisSensorType.ZPhase:
                RunZPhase();
                break;
            default:
                throw new ValueError();
        }

        _axis.SetPosition(0.0);
        _axis.SetSpeed(_axis.GetJogSpeed(JogSpeedLevel.Fast));
        _homePosition.Value.MoveAndWait();
    }

    public void RunZPhase()
    {
        _axis.SearchZPhase(0);
    }

    public void RunNormal()
    {
        try
        {
            _axis.SetSpeed(_initSpeed);
            _axis.VelocityMove(_direction);
            _axis.WaitSensor(_sensorType, true, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorEntryTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            _axis.EStop();
        }

        try
        {
            var halfHomingSpeed = (SpeedProfile)_initSpeed.Value.Clone();
            halfHomingSpeed.Velocity /= 10;
            _axis.SetSpeed(halfHomingSpeed);
            _axis.VelocityMove((AxisDirection)((int)_direction * -1));
            _axis.WaitSensor(_sensorType, false, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorExitTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            _axis.EStop();
        }

        try
        {
            _axis.VelocityMove(_direction);
            _axis.WaitSensor(_sensorType, true, 3 * 60 * 1000);
        }
        catch (TimeoutError)
        {
            SensorReentryTimeout.Show();
            throw new SequenceError();
        }
        finally
        {
            _axis.EStop();
        }
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        // TODO
    }
}