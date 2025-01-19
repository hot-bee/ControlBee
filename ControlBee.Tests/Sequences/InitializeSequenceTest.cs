using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(InitializeSequence))]
public class InitializeSequenceTest
{
    [Fact]
    public void NormalTest()
    {
        using var timeManager = new FrozenTimeManager();
        var tester = new ScenarioFlowTester();
        var axisX = new FakeAxis(timeManager, tester);

        var actorFactory = new ActorFactory(
            new EmptyAxisFactory(),
            new EmptyVariableManager(),
            timeManager
        );
        var testActor = actorFactory.Create<TestActor>("testActor", axisX);

        testActor.HomingSpeed.Value.Velocity = 0.1;
        testActor.HomePosition.Value[0] = 10.0;

        tester.Setup(
            [
                new ConditionStep(() => testActor.X.GetPosition() < -0.1),
                new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                new ConditionStep(() => testActor.X.GetPosition() > -0.08),
                new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, false)),
                new ConditionStep(() => testActor.X.GetPosition() < -0.09),
                new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                new ConditionStep(() => testActor.X.GetPosition() == 10.0),
            ]
        );

        testActor.InitializeSequence.Run();
        tester.Complete.Should().BeTrue();
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position1D> HomePosition = new(VariableScope.Global);
        public readonly Variable<SpeedProfile> HomingSpeed = new(VariableScope.Global);
        public readonly InitializeSequence InitializeSequence;
        public readonly IAxis X;

        public TestActor(ActorConfig config, IAxis axisX)
            : base(config)
        {
            X = axisX;
            PositionAxesMap.Add(HomePosition, [X]);
            InitializeSequence = new InitializeSequence(X, HomingSpeed, HomePosition);
        }
    }
}
