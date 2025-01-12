using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position4D : Position
{
    public Position4D()
    {
        InternalVector = new DenseVector(Rank);
    }

    protected sealed override int Rank => 4;
}
