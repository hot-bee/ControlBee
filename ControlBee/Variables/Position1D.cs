using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position1D : Position
{
    public Position1D()
    {
        InternalVector = new DenseVector(Rank);
    }

    protected sealed override int Rank => 1;
}
