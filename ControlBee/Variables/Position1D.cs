using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position1D(DenseVector vector) : Position(vector)
{
    public Position1D()
        : this(DenseVector.OfArray([0]))
    {
    }

    private Position1D(Position1D other) : this((DenseVector)other.Vector.Clone())
    {
    }

    protected sealed override int Rank => 1;

    public override object Clone()
    {
        return new Position1D(this);
    }
}