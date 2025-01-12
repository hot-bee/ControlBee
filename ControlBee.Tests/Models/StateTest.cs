using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(State<>))]
public class StateTest
{
    [Fact]
    public void StateTransitionTest()
    {
        var config = new ActorConfig("testActor", new EmptyVariableManager());
        var actor = new TestPickerActor(config);

        actor.Start();
        actor.Send(new Message(Actor.Empty, "Pickup"));
        actor.Send(new Message(Actor.Empty, "_terminate"));
        actor.Join();
        actor.PickupCount.Value.Should().Be(1);
    }

    public class TestPickerActor : Actor
    {
        public Variable<int> PickupCount = new(VariableScope.Local);

        public TestPickerActor(ActorConfig config)
            : base(config)
        {
            State = new IdleState(this);
        }
    }

    public class IdleState(TestPickerActor actor) : State<TestPickerActor>(actor)
    {
        public override IState ProcessMessage(Message message)
        {
            if (message.Payload as string == "Pickup")
                return new PickupState(Actor);
            return this;
        }
    }

    public class PickupState(TestPickerActor actor) : State<TestPickerActor>(actor)
    {
        public override IState ProcessMessage(Message message)
        {
            Actor.PickupCount.Value++;
            return this;
        }
    }
}
