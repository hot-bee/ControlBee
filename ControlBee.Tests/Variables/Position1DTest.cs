using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position1D))]
public class Position1DTest
{
    [Fact]
    public void InitialValuesTest()
    {
        var position = new Position1D(DenseVector.OfArray([1]));
        Assert.Equal(DenseVector.OfArray([1]), position.Vector);
    }
}
