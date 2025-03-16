using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

#pragma warning disable xUnit1031

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace ControlBee.Tests.Services;

[TestSubject(typeof(FrozenTimeManager))]
public class FrozenTimeManagerTest : ActorFactoryBase
{
    [Fact]
    public async Task RunTaskTest()
    {
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("Actor");

        var called = false;
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "RegisteredThreadsCount",
            message =>
            {
                var registeredThreadsCount = (int)message.Payload!;
                Assert.Equal(2, registeredThreadsCount);
                called = true;
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "RunTaskDone",
            _ =>
            {
                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(new Message(client, "RunTask"));
        actor.Join();

        var frozenTimeManager = (FrozenTimeManager)TimeManager;
        Assert.Equal(0, frozenTimeManager.RegisteredThreadsCount);
        Assert.Equal(10.0, actor.X.GetPosition());
        Assert.True(called);
    }

    [Fact]
    public void RunTaskAndEmptyActorTest()
    {
        var testActor = ActorFactory.Create<TestActor>("testActor");
        var axisX = (FakeAxis)testActor.X;

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
            ]
        );

        testActor.Start();
        testActor.Send(new Message(EmptyActor.Instance, "_initialize"));
        testActor.Send(new Message(EmptyActor.Instance, "_terminate"));
        testActor.Join();
        ScenarioFlowTester.Complete.Should().BeTrue();
    }

    [Fact]
    public async Task CancelTaskTest()
    {
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        using var frozenTimeManager = new FrozenTimeManager(
            SystemConfigurations,
            scenarioFlowTester
        );
        var fakeAxis = new FakeAxis(DeviceManager, frozenTimeManager, scenarioFlowTester);

        var cancellationTokenSource = new CancellationTokenSource();
        var task = frozenTimeManager.RunTask(() =>
        {
            while (true)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }
            // ReSharper disable once FunctionNeverReturns
        });
        await cancellationTokenSource.CancelAsync();
        try
        {
            await task;
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            Assert.Equal(0, frozenTimeManager.RegisteredThreadsCount);
        }
    }

    [Fact]
    public void SleepTest()
    {
        var testActor = ActorFactory.Create<TestActor>("testActor");

        ScenarioFlowTester.Setup(
            [
                [
                    new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(
                        () => testActor.Send(new Message(EmptyActor.Instance, "_terminate"))
                    ),
                ],
            ]
        );

        testActor.Start();
        testActor.Send(new Message(EmptyActor.Instance, "Sleep"));
        testActor.Join();
    }

    [Fact]
    public void GetEventKeyTest()
    {
        var thread = new Thread(() =>
        {
            var key = FrozenTimeManager.GetEventKey();
            Assert.Equal(FrozenTimeManager.KeyType.Thread, key.Item1);
        });
        thread.Start();
        thread.Join();

        var task = Task.Run(() =>
        {
            var key = FrozenTimeManager.GetEventKey();
            Assert.Equal(FrozenTimeManager.KeyType.Task, key.Item1);
        });
        task.Wait();
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
            X = config.AxisFactory.Create();
            PositionAxesMap.Add(HomePositionX, [X]);
            InitializeSequenceX = new InitializeSequence(X, HomingSpeedX, HomePositionX);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "_initialize":
                    TimeManager
                        .RunTask(() =>
                        {
                            InitializeSequenceX.Run();
                            return 0;
                        })
                        .Wait();
                    return true;
                case "Sleep":
                    TimeManager.Sleep(100);
                    Send(new Message(this, "Sleep"));
                    return true;
                case "RunTask":
                {
                    var task = TimeManager.RunTask(() =>
                    {
                        var frozenTimeManager = (FrozenTimeManager)TimeManager;
                        message.Sender.Send(
                            new Message(
                                message,
                                this,
                                "RegisteredThreadsCount",
                                frozenTimeManager.RegisteredThreadsCount
                            )
                        );
                        X.SetSpeed(new SpeedProfile { Velocity = 1.0 });
                        X.MoveAndWait(10.0);
                    });
                    task.Wait();
                    message.Sender.Send(new Message(message, this, "RunTaskDone"));
                    return true;
                }
            }

            return false;
        }
    }
}
