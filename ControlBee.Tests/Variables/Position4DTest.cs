using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position4D))]
public class Position4DTest
{
    [Fact]
    public void InitialValuesTest()
    {
        var position = new Position4D(DenseVector.OfArray([1, 2, 3, 4]));
        Assert.Equal(DenseVector.OfArray([1, 2, 3, 4]), position.Vector);
    }
}
