using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(InitializeSequence))]
public class InitializeSequenceTest : ActorFactoryBase
{
    [Fact]
    public void NormalTest()
    {
        var testActor = ActorFactory.Create<TestActor>("testActor");
        var axisX = (FakeAxis)testActor.X;
        var axisY = (FakeAxis)testActor.Y;
        var axisZ = (FakeAxis)testActor.Z;

        ScenarioFlowTester.Setup(
            [
                [
                    new ConditionStep(() => testActor.X.GetPosition() < -0.1),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.X.GetPosition() > -0.08),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, false)),
                    new ConditionStep(() => testActor.X.GetPosition() < -0.09),
                    new BehaviorStep(() => axisX.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.X.GetPosition() == 10.0),
                ],
                [
                    new ConditionStep(() => testActor.Y.GetPosition() < -0.1),
                    new BehaviorStep(() => axisY.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.Y.GetPosition() > -0.08),
                    new BehaviorStep(() => axisY.SetSensorValue(AxisSensorType.Home, false)),
                    new ConditionStep(() => testActor.Y.GetPosition() < -0.09),
                    new BehaviorStep(() => axisY.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.Y.GetPosition() == 10.0),
                ],
                [
                    new ConditionStep(() => testActor.Z.GetPosition() < -0.1),
                    new BehaviorStep(() => axisZ.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.Z.GetPosition() > -0.08),
                    new BehaviorStep(() => axisZ.SetSensorValue(AxisSensorType.Home, false)),
                    new ConditionStep(() => testActor.Z.GetPosition() < -0.09),
                    new BehaviorStep(() => axisZ.SetSensorValue(AxisSensorType.Home, true)),
                    new ConditionStep(() => testActor.Z.GetPosition() == 10.0),
                ],
            ]
        );

        testActor.Start();
        testActor.Send(new Message(EmptyActor.Instance, "_initialize"));
        testActor.Send(new Message(EmptyActor.Instance, "_terminate"));
        testActor.Join();
        ScenarioFlowTester.Complete.Should().BeTrue();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private class TestActor : Actor
    {
        public readonly Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );

        public readonly Variable<Position1D> HomePositionY = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );

        public readonly Variable<Position1D> HomePositionZ = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );

        public readonly Variable<SpeedProfile> HomingSpeedX = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 1.0 }
        );

        public readonly Variable<SpeedProfile> HomingSpeedY = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 1.0 }
        );

        public readonly Variable<SpeedProfile> HomingSpeedZ = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 1.0 }
        );

        public readonly InitializeSequence InitializeSequenceX;
        public readonly InitializeSequence InitializeSequenceY;
        public readonly InitializeSequence InitializeSequenceZ;

        public readonly IAxis X;
        public readonly IAxis Y;
        public readonly IAxis Z;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
            Y = AxisFactory.Create();
            Z = AxisFactory.Create();

            PositionAxesMap.Add(HomePositionX, [X]);
            PositionAxesMap.Add(HomePositionY, [Y]);
            PositionAxesMap.Add(HomePositionZ, [Z]);

            InitializeSequenceX = new InitializeSequence(X, HomingSpeedX, HomePositionX);
            InitializeSequenceY = new InitializeSequence(Y, HomingSpeedY, HomePositionY);
            InitializeSequenceZ = new InitializeSequence(Z, HomingSpeedZ, HomePositionZ);
        }

        protected override void ProcessMessage(Message message)
        {
            if (message.Name == "_initialize")
            {
                InitializeSequenceZ.Run();
                Task.WaitAll(
                    TimeManager.RunTask(() =>
                    {
                        InitializeSequenceX.Run();
                        return 0;
                    }),
                    TimeManager.RunTask(() =>
                    {
                        InitializeSequenceY.Run();
                        return 0;
                    })
                );
            }
        }
    }
}
