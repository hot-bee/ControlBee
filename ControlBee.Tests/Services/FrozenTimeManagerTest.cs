using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace ControlBee.Tests.Services;

[TestSubject(typeof(FrozenTimeManager))]
public class FrozenTimeManagerTest
{
    [Fact]
    public async Task RunTaskTest()
    {
        using var frozenTimeManager = new FrozenTimeManager();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var fakeAxis = new FakeAxis(frozenTimeManager, scenarioFlowTester);

        var task = frozenTimeManager.RunTask(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.Equal(1, frozenTimeManager.RegisteredThreadsCount);
            fakeAxis.SetSpeed(new SpeedProfile { Velocity = 1.0 });
            fakeAxis.MoveAndWait(10.0);
        });
        await task;
        Assert.Equal(0, frozenTimeManager.RegisteredThreadsCount);
        Assert.Equal(10.0, fakeAxis.GetPosition(PositionType.Command));
    }

    [Fact]
    public void RunTaskAndEmptyActorTest()
    {
        using var frozenTimeManager = new FrozenTimeManager();
        var scenarioFlowTester = new ScenarioFlowTester();

        var systemConfigurations = new SystemConfigurations { FakeMode = true };
        var deviceManager = Mock.Of<IDeviceManager>();
        var axisFactory = new AxisFactory(
            systemConfigurations,
            deviceManager,
            frozenTimeManager,
            scenarioFlowTester
        );
        var actorFactory = new ActorFactory(
            axisFactory,
            EmptyDigitalInputFactory.Instance,
            EmptyDigitalOutputFactory.Instance,
            EmptyVariableManager.Instance,
            frozenTimeManager,
            EmptyActorItemInjectionDataSource.Instance,
            Mock.Of<IActorRegistry>()
        );
        var testActor = actorFactory.Create<TestActor>("testActor");
        var axisX = (FakeAxis)testActor.X;

        scenarioFlowTester.Setup(
            new ISimulationStep[][]
            {
                [
                    new ConditionStep(() => testActor.X.GetPosition() < -0.1),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.X.GetPosition() > -0.08),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, false)),
                    new ConditionStep(() => testActor.X.GetPosition() < -0.09),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.X.GetPosition() == 10.0),
                ],
            }
        );

        testActor.Start();
        testActor.Send(new Message(EmptyActor.Instance, "_initialize"));
        testActor.Send(new Message(EmptyActor.Instance, "_terminate"));
        testActor.Join();
        scenarioFlowTester.Complete.Should().BeTrue();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private class TestActor : Actor
    {
        public readonly Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );

        public readonly Variable<SpeedProfile> HomingSpeedX = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 1.0 }
        );

        public readonly InitializeSequence InitializeSequenceX;

        public readonly IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
            PositionAxesMap.Add(HomePositionX, [X]);
            InitializeSequenceX = new InitializeSequence(X, HomingSpeedX, HomePositionX);
        }

        protected override void ProcessMessage(Message message)
        {
            if (message.Name == "_initialize")
                TimeManager.RunTask(() => InitializeSequenceX.Run()).Wait();
        }
    }
}
