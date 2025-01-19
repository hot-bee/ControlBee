using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position2D(DenseVector vector) : Position(vector)
{
    public Position2D()
        : this(DenseVector.OfArray([0, 0])) { }

    protected sealed override int Rank => 2;
}
