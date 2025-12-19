using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position3D(DenseVector vector) : Position(vector)
{
    public Position3D()
        : this(DenseVector.OfArray([0, 0, 0]))
    {
    }

    private Position3D(Position3D other) : this((DenseVector)other.Vector.Clone())
    {
    }

    protected sealed override int Rank => 3;

    public override object Clone()
    {
        return new Position3D(this);
    }
}