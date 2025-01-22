﻿using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
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
        var config = new ActorConfig(
            "testActor",
            EmptyAxisFactory.Instance,
            EmptyDigitalInputFactory.Instance,
            EmptyDigitalOutputFactory.Instance,
            EmptyVariableManager.Instance,
            new TimeManager()
        );
        var actor = new TestPickerActor(config);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Pickup"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
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
            if (message.Name == "Pickup")
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
