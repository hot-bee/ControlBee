using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position3D : Position
{
    public Position3D()
    {
        InternalVector = new DenseVector(Rank);
    }

    protected sealed override int Rank => 3;
}
