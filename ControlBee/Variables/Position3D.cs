using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position3D(DenseVector vector) : Position(vector)
{
    public Position3D()
        : this(DenseVector.OfArray([0, 0, 0])) { }

    protected sealed override int Rank => 3;
}
