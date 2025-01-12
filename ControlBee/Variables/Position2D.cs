using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public class Position2D : Position
{
    public Position2D()
    {
        InternalVector = new DenseVector(Rank);
    }

    public Position2D(DenseVector vector)
    {
        if (vector.Count != Rank)
            throw new ApplicationException();
        InternalVector = DenseVector.OfVector(vector);
    }

    protected sealed override int Rank => 2;
}
