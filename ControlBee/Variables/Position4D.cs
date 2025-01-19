using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position4D(DenseVector vector) : Position(vector)
{
    public Position4D()
        : this(DenseVector.OfArray([0, 0, 0, 0])) { }

    protected sealed override int Rank => 4;
}
