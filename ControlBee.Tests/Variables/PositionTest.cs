using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
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
        actor.Start();
        actor.Send(new Message(actor, "Go"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.True(actor.MyPosition.Value.IsNear([10, 20], 1));
        Assert.False(actor.MyPosition.Value.IsNear([10, 0], 1));
        Assert.False(actor.MyPosition.Value.IsNear([0, 0], 1));
    }

    [Fact]
    public void WaitForPositionTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.Start();
        actor.Send(new Message(actor, "GoAndStop"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.True(actor.X.GetPosition() is > 1 and < 5);
        Assert.True(actor.Y.GetPosition() is > 1 and < 5);
    }

    public class TestActor : Actor
    {
        public Variable<Position2D> MyPosition = new(
            VariableScope.Global,
            new Position2D(DenseVector.OfArray([10, 20]))
        );

        public Variable<SpeedProfile> Speed = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 10 }
        );
        public IAxis X;
        public IAxis Y;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Y = config.AxisFactory.Create();

            PositionAxesMap.Add(MyPosition, [X, Y]);
        }

        protected override void ProcessMessage(Message message)
        {
            base.ProcessMessage(message);
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
                    MyPosition.Value.Move();
                    MyPosition.Value.WaitForPosition(PositionComparisonType.Greater, [2, 2]);
                    MyPosition.Value.Stop();
                    break;
            }
        }
    }
}
