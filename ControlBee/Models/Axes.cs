using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using log4net;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Models;

public class Axes
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Axes));
    private IAxis[] _axes = [];

    public Axes()
    {
        // TODO
    }

    public Axes(string axes)
    {
        // TODO
    }

    public Axes(IAxis[] axes)
    {
        _axes = axes;
    }

    public void Move(Position2D position)
    {
        // TODO
    }

    public void Move(DenseVector position)
    {
        // TODO
    }

    public void SetSpeed(IVariable[] speeds)
    {
        if (speeds.Length != _axes.Length)
            throw new ApplicationException();

        for (var i = 0; i < _axes.Length; i++)
            _axes[i].SetSpeed(speeds[i]);
    }

    public void SetAxes(IAxis[] axes)
    {
        _axes = axes;
    }

    public void Wait()
    {
        foreach (var axis in _axes) axis.Wait();
    }

    public bool IsMoving()
    {
        return _axes.Any(x => x.IsMoving());
    }

    public void Move(double[] positions, SpeedProfile speedProfile)
    {
        if (_axes[0].GetDevice() is not IMotionDevice motionDevice)
        {
            Logger.Error($"Couldn't find motionDevice. ({_axes[0].Actor}, {_axes[0].ItemPath})");
            return;
        }

        if (_axes.Length != positions.Length)
        {
            Logger.Error($"_axes length and position length mismatch. ({_axes[0].Actor}, {_axes[0].ItemPath}).");
            return;
        }

        var resolutionOfFirstAxis = Math.Abs(_axes[0].ResolutionValue);
        motionDevice.InterpolateMove(
            _axes.Select((t, i) => (t.GetChannel(), positions[i] * t.ResolutionValue)).ToArray(),
            speedProfile.Velocity * resolutionOfFirstAxis, speedProfile.Accel * resolutionOfFirstAxis,
            speedProfile.Decel * resolutionOfFirstAxis, speedProfile.AccelJerkRatio, speedProfile.DecelJerkRatio);
    }
}