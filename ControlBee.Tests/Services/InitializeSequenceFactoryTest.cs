using System.Net;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(InitializeSequenceFactory))]
public class InitializeSequenceFactoryTest : ActorFactoryBase
{
    [Theory]
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateFromActor(bool fakeMode)
    {
        SystemConfigurations = new SystemConfigurations() { FakeMode = fakeMode };
        DeviceManager = new DeviceManager();
        ScenarioFlowTester = new ScenarioFlowTester();
        AxisFactory = new AxisFactory(
            SystemConfigurations,
            DeviceManager,
            TimeManager,
            ScenarioFlowTester
        );
        ActorRegistry = new ActorRegistry();
        VariableManager = new VariableManager(Database, ActorRegistry);
        DigitalInputFactory = new DigitalInputFactory(
            SystemConfigurations,
            DeviceManager,
            ScenarioFlowTester
        );
        DigitalOutputFactory = new DigitalOutputFactory(
            SystemConfigurations,
            DeviceManager,
            TimeManager
        );
        InitializeSequenceFactory = new InitializeSequenceFactory(SystemConfigurations);
        ActorItemInjectionDataSource = new ActorItemInjectionDataSource();
        ActorFactory = new ActorFactory(
            SystemConfigurations,
            AxisFactory,
            DigitalInputFactory,
            DigitalOutputFactory,
            InitializeSequenceFactory,
            VariableManager,
            TimeManager,
            ScenarioFlowTester,
            ActorItemInjectionDataSource,
            ActorRegistry
        );

        var actor = ActorFactory.Create<TestActor>("MyActor");
        if (fakeMode)
            Assert.IsType<FakeInitializeSequence>(actor.InitializeSequenceX);
        else
            Assert.IsType<InitializeSequence>(actor.InitializeSequenceX);
    }

    public class TestActor : Actor
    {
        public Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );
        public Variable<SpeedProfile> HomeSpeedX = new();
        public IInitializeSequence InitializeSequenceX;
        public IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
            InitializeSequenceX = InitializeSequenceFactory.Create(X, HomeSpeedX, HomePositionX);
        }
    }
}
