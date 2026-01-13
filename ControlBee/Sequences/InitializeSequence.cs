using System.Diagnostics;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Exceptions;
using log4net;

namespace ControlBee.Sequences;

public class InitializeSequence(
    IAxis axis,
    Variable<SpeedProfile> initSpeed,
    Variable<Position1D> homePosition,
    AxisSensorType sensorType,
    AxisDirection direction
) : ActorItem, IInitializeSequence
{
    private static readonly ILog Logger = LogManager.GetLogger("Sequence");

    public Variable<int> DelayBeforeClearPosition = new(VariableScope.Global, 0);
    public IDialog SensorEntryTimeout = new DialogPlaceholder();
    public IDialog SensorExitTimeout = new DialogPlaceholder();
    public IDialog SensorReentryTimeout = new DialogPlaceholder();

    public void Run()
    {
        if (axis.GetDevice() == null)
        {
            Logger.Warn(
                $"Skip initialize because device is null. ({axis.Actor.Name}, {axis.ItemPath})"
            );
            return;
        }
        axis.ClearAlarm();
        axis.Enable(false);
        axis.Enable(true);
        axis.OnBeforeInitialize();
        axis.SetSoftwareLimit(false, 0.0, 0.0);

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

        var useSoftwareLimit = axis.GetUseSoftwareLimit();
        var negativeSoftwareLimitPosition = axis.GetNegativeSoftwareLimitPosition();
        var positiveSoftwareLimitPosition = axis.GetPositiveSoftwareLimitPosition();
        axis.SetSoftwareLimit(
            useSoftwareLimit,
            negativeSoftwareLimitPosition,
            positiveSoftwareLimitPosition
        );

        axis.SetSpeed(axis.GetJogSpeed(JogSpeedLevel.Fast));
        homePosition.Value.MoveAndWait();
    }

    public void RunZPhase()
    {
        axis.SearchZPhase(0);
    }

    public void SearchEntry(bool reverse)
    {
        var timeoutWatch = new Stopwatch();
        var searchDirection = direction;
        if (reverse)
            searchDirection = (AxisDirection)((int)direction * -1);
        try
        {
            axis.SetSpeed(initSpeed);
            if (!axis.GetSensorValueOrFalse(sensorType))
                axis.VelocityMove(searchDirection);
            timeoutWatch.Restart();
            while (true)
            {
                if (axis.GetSensorValueOrTrue(sensorType))
                    break;
                if (
                    !reverse
                    && sensorType == AxisSensorType.Home
                    && (
                        axis.GetSensorValue(AxisSensorType.PositiveLimit)
                        || axis.GetSensorValue(AxisSensorType.NegativeLimit)
                    )
                )
                    throw new LimitTouchException();
                if (timeoutWatch.ElapsedMilliseconds > 3 * 60 * 1000)
                    throw new TimeoutError();
                TimeManager.Sleep(1);
            }
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
    }

    public void RunNormal()
    {
        try
        {
            SearchEntry(reverse: false);
        }
        catch (LimitTouchException)
        {
            SearchEntry(reverse: true);
            SearchExit(slowSpeed: false);
            SearchEntry(reverse: false);
        }

        SearchExit(slowSpeed: true);
        SearchReentry();
    }

    private void SearchReentry()
    {
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

    private void SearchExit(bool slowSpeed)
    {
        try
        {
            var halfHomingSpeed = (SpeedProfile)initSpeed.Value.Clone();
            if (slowSpeed)
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
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        // TODO
    }

    public class LimitTouchException : Exception { }
}
