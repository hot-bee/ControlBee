using System.Threading.Tasks;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.TestUtils;
using ControlBeeAbstract.Constants;
using ControlBeeTest.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(InitializeSequence))]
public class InitializeSequenceTest : ActorFactoryBase
{
    [Fact]
    public void NormalTest()
    {
        Recreate(
            new ActorFactoryBaseConfig
            {
                SystemConfigurations = new SystemConfigurations { FakeMode = true },
            }
        );

        var testActor = ActorFactory.Create<TestActor>("testActor");
        var axisX = (FakeAxis)testActor.X;
        var axisY = (FakeAxis)testActor.Y;
        var axisZ = (FakeAxis)testActor.Z;

        ScenarioFlowTester.Setup([
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
        ]);

        testActor.Start();
        testActor.Send(new Message(EmptyActor.Instance, "_initialize"));
        testActor.Send(new Message(EmptyActor.Instance, "_terminate"));
        testActor.Join();
        Assert.IsTrue(ScenarioFlowTester.Complete);
    }

    private class TestActor : Actor
    {
        public readonly IAxis X;
        public readonly IAxis Y;
        public readonly IAxis Z;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Y = config.AxisFactory.Create();
            Z = config.AxisFactory.Create();

            X.GetInitPos()[0] = 10.0;
            Y.GetInitPos()[0] = 10.0;
            Z.GetInitPos()[0] = 10.0;

            X.GetInitSpeed().Velocity = 1.0;
            Y.GetInitSpeed().Velocity = 1.0;
            Z.GetInitSpeed().Velocity = 1.0;

            ((Axis)X).InitDirection = AxisDirection.Negative;
            ((Axis)Y).InitDirection = AxisDirection.Negative;
            ((Axis)Z).InitDirection = AxisDirection.Negative;
        }

        protected override bool ProcessMessage(Message message)
        {
            if (message.Name == "_initialize")
            {
                Z.Initialize();
                Task.WaitAll(
                    TimeManager.RunTask(() =>
                    {
                        X.Initialize();
                        return 0;
                    }),
                    TimeManager.RunTask(() =>
                    {
                        Y.Initialize();
                        return 0;
                    })
                );
                return true;
            }

            return base.ProcessMessage(message);
        }
    }
}
