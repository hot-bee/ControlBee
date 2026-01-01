using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position2D(DenseVector vector) : Position(vector)
{
    public Position2D()
        : this(DenseVector.OfArray([0, 0])) { }

    private Position2D(Position2D other)
        : this((DenseVector)other.Vector.Clone()) { }

    protected sealed override int Rank => 2;

    public override object Clone()
    {
        return new Position2D(this);
    }
}
