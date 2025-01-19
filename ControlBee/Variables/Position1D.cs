using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position1D(DenseVector vector) : Position(vector)
{
    public Position1D()
        : this(DenseVector.OfArray([0])) { }

    protected sealed override int Rank => 1;
}
