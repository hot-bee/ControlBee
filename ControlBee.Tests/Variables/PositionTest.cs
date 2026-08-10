using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position))]
public class PositionTest : ActorFactoryBase
{
    [Fact]
    public void IsNearTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.False(actor.MyPosition.Value.IsNear(1));

        actor.Start();
        actor.Send(new Message(actor, "Go"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.True(actor.MyPosition.Value.IsNear(1));
    }

    [Fact]
    public void MoveToSavedPos_NotifiesRequesterAndOwningActor()
    {
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        var clientDone = false;
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "MoveToSavedPosDone",
            _ => clientDone = true
        );

        actor.Start();
        actor.Send(new ActorItemMessage(client, "/MyPosition", "MoveToSavedPos"));
        actor.Join();

        Assert.True(actor.MoveToSavedPosDoneReceived);
        Assert.True(clientDone);
        Assert.True(actor.MyPosition.Value.IsNear(1));
    }

    [Fact]
    public void MoveToHomePos_NotifiesRequesterAndOwningActor()
    {
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        var clientDone = false;
        ActorUtils.SetupActionOnGetMessage(
            actor,
            client,
            "MoveToHomePosDone",
            _ => clientDone = true
        );

        actor.Start();
        actor.Send(new ActorItemMessage(client, "/MyPosition", "MoveToHomePos"));
        actor.Join();

        Assert.True(actor.MoveToHomePosDoneReceived);
        Assert.True(clientDone);
    }

    [Fact]
    public void WaitForPositionTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.Start();
        actor.Send(new Message(actor, "GoAndStop"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.True(actor.X.GetPosition() is > 10 and < 30);
        Assert.True(actor.Y.GetPosition() is > 10 and < 30);
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position2D> MyPosition = new(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([10, 20]))
        );

        public readonly Variable<SpeedProfile> Speed = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 10 }
        );

        public readonly IAxis X;
        public readonly IAxis Y;
        public bool MoveToSavedPosDoneReceived;
        public bool MoveToHomePosDoneReceived;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Y = config.AxisFactory.Create();

            PositionAxesMap.Add(MyPosition, [X, Y]);
            X.Enable(true);
            Y.Enable(true);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "MoveToSavedPosDone":
                    MoveToSavedPosDoneReceived = true;
                    Send(new TerminateMessage());
                    break;
                case "MoveToHomePosDone":
                    MoveToHomePosDoneReceived = true;
                    Send(new TerminateMessage());
                    break;
            }

            return base.ProcessMessage(message);
        }

        protected override void MessageHandler(Message message)
        {
            base.MessageHandler(message);
            switch (message.Name)
            {
                case "Go":
                    X.SetSpeed(Speed);
                    Y.SetSpeed(Speed);
                    MyPosition.Value.MoveAndWait();
                    break;
                case "GoAndStop":
                    X.SetSpeed(Speed);
                    Y.SetSpeed(Speed);
                    X.Move(100);
                    Y.Move(100);
                    MyPosition.Value.WaitForPosition(PositionComparisonType.Greater);
                    MyPosition.Value.Stop();
                    break;
            }
        }
    }
}
