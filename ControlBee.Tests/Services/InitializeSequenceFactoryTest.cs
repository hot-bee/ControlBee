using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(InitializeSequenceFactory))]
public class InitializeSequenceFactoryTest
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
        var database = Mock.Of<IDatabase>();
        var systemConfigurations = new SystemConfigurations { FakeMode = fakeMode };
        var deviceManager = new DeviceManager();
        using var timeManager = new FrozenTimeManager();
        var scenarioFlowTester = new ScenarioFlowTester();
        var axisFactory = new AxisFactory(
            systemConfigurations,
            deviceManager,
            timeManager,
            scenarioFlowTester
        );
        var variableManager = new VariableManager(database);
        var digitalInputFactory = new DigitalInputFactory(
            systemConfigurations,
            deviceManager,
            scenarioFlowTester
        );
        var digitalOutputFactory = new DigitalOutputFactory(systemConfigurations, deviceManager);
        var initializeSequenceFactory = new InitializeSequenceFactory(systemConfigurations);
        var actorItemInjectionDataSource = EmptyActorItemInjectionDataSource.Instance;
        var actorRegistry = new ActorRegistry();
        var actorFactory = new ActorFactory(
            axisFactory,
            digitalInputFactory,
            digitalOutputFactory,
            initializeSequenceFactory,
            variableManager,
            timeManager,
            actorItemInjectionDataSource,
            actorRegistry
        );

        var actor = actorFactory.Create<TestActor>("MyActor");
        if (fakeMode)
            Assert.IsType<FakeInitializeSequence>(actor.InitializeSequenceX);
        else
            Assert.IsType<InitializeSequence>(actor.InitializeSequenceX);
    }

    public class TestActor : Actor
    {
        public Variable<Position1D> HomePositionX = new();
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
