using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position4D(DenseVector vector) : Position(vector)
{
    public Position4D()
        : this(DenseVector.OfArray([0, 0, 0, 0]))
    {
    }

    private Position4D(Position4D other) : this((DenseVector)other.Vector.Clone())
    {
    }

    protected sealed override int Rank => 4;

    public override object Clone()
    {
        return new Position4D(this);
    }
}