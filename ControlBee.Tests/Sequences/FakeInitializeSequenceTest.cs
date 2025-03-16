using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(FakeInitializeSequence))]
public class FakeInitializeSequenceTest : ActorFactoryBase
{
    [Fact]
    public void RunTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Assert.Equal(100.0, actor.X.GetPosition());
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([100.0]))
        );

        public readonly Variable<SpeedProfile> HomeSpeedX = new(
            VariableScope.Global,
            new SpeedProfile { Velocity = 10.0 }
        );

        public readonly IInitializeSequence InitializeSequenceX;
        public readonly IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            PositionAxesMap.Add(HomePositionX, [X]);
            InitializeSequenceX = config.InitializeSequenceFactory.Create(
                X,
                HomeSpeedX,
                HomePositionX
            );
        }

        protected override void MessageHandler(Message message)
        {
            base.MessageHandler(message);
            if (message.Name == "Go")
                InitializeSequenceX.Run();
        }
    }
}
