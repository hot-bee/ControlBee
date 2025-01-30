using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ActorBuiltinMessageHandler))]
public class ActorBuiltinMessageHandlerTest : ActorFactoryBase
{
    [Fact]
    public void InitializeAxisTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.State = new IdleState(actor);

        actor.Start();
        actor.Send(new Message(actor, "_initializeAxis", "X"));
        actor.Send(new Message(actor, "_terminate"));
        actor.Join();

        Assert.IsType<EmptyState>(actor.State);
    }

    [Fact]
    public void ResetStateTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.State = new IdleState(actor);

        actor.Start();
        actor.Send(new Message(actor, "_resetState"));
        actor.Send(new Message(actor, "_terminate"));
        actor.Join();

        Assert.IsType<EmptyState>(actor.State);
    }

    public class TestActor : Actor
    {
        public Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );
        public Variable<SpeedProfile> HomeSpeedX = new();
        public IInitializeSequence InitializeSequenceX;
        public IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            InitializeSequenceX = config.InitializeSequenceFactory.Create(
                X,
                HomeSpeedX,
                HomePositionX
            );
            X.SetInitializeAction(() => throw new FatalSequenceError());
        }
    }

    public class IdleState(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            return false;
        }
    }
}
