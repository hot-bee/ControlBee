using ControlBee.Interfaces;
using ControlBee.Variables;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Models;

public class Axes
{
    private Axis[] _axes = [];

    public Axes()
    {
        // TODO
    }

    public Axes(string axes)
    {
        // TODO
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

    public void SetAxes(Axis[] axes)
    {
        _axes = axes;
    }
}
