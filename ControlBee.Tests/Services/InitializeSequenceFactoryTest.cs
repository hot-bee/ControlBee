using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(InitializeSequenceFactory))]
public class InitializeSequenceFactoryTest : ActorFactoryBase
{
    [Theory(Skip = "We don't use InitializeSequenceFactory anymore.")]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateTest(bool fakeMode)
    {
        var systemConfigurations = new SystemConfigurations { FakeMode = fakeMode };
        var initializeSequenceFactory = new InitializeSequenceFactory(systemConfigurations);
        var sequence = initializeSequenceFactory.Create(
            Mock.Of<IAxis>(),
            new SpeedProfile(),
            new Position1D()
        );
        if (fakeMode)
            Assert.IsType<FakeInitializeSequence>(sequence);
        else
            Assert.IsType<InitializeSequence>(sequence);
    }

    [Theory(Skip = "We don't use InitializeSequenceFactory anymore.")]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateFromActor(bool fakeMode)
    {
        Recreate(
            new ActorFactoryBaseConfig
            {
                SystemConfigurations = new SystemConfigurations { FakeMode = fakeMode },
            }
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");
        if (fakeMode)
            Assert.IsType<FakeInitializeSequence>(actor.InitializeSequenceX);
        else
            Assert.IsType<InitializeSequence>(actor.InitializeSequenceX);
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );

        public readonly Variable<SpeedProfile> HomeSpeedX = new();
        public readonly IInitializeSequence InitializeSequenceX;
        public readonly IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            InitializeSequenceX = config.InitializeSequenceFactory.Create(
                X,
                HomeSpeedX,
                HomePositionX
            );
        }
    }
}
