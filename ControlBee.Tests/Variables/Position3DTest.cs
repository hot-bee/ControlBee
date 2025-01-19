using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position3D))]
public class Position3DTest
{
    [Fact]
    public void InitialValuesTest()
    {
        var position = new Position3D(DenseVector.OfArray([1, 2, 3]));
        Assert.Equal(DenseVector.OfArray([1, 2, 3]), position.Vector);
    }
}
