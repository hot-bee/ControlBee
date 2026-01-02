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

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Y = config.AxisFactory.Create();

            PositionAxesMap.Add(MyPosition, [X, Y]);
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
