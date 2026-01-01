using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorStateTest : ActorFactoryBase
{
    [Fact]
    public void CheckpointTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");

        var called = false;
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            Assert.IsType<StateA>(oldState);
            Assert.IsType<StateB>(newState);
            called = true;
        };

        ScenarioFlowTester.Setup([
            [
                new BehaviorStep(() =>
                    actor.Send(new Message(EmptyActor.Instance, "TransitToStateB"))
                ),
                new ConditionStep(() => actor.State.GetType() == typeof(StateB)),
                new BehaviorStep(() => actor.Send(new TerminateMessage())),
            ],
        ]);

        actor.Start();
        actor.Join();
        Assert.True(ScenarioFlowTester.Complete);
        Assert.True(called);
    }

    [Fact]
    public void StateChangeWithErrorTest()
    {
        var ui = MockActorFactory.Create("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        var called = false;
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            Assert.IsType<StateA>(oldState);
            Assert.IsType<ErrorState<TestActor>>(newState);
            called = true;
        };
        ScenarioFlowTester.Setup([
            [
                new BehaviorStep(() => actor.Send(new Message(EmptyActor.Instance, "RaiseError"))),
                new ConditionStep(() => actor.State.GetType() == typeof(ErrorState<TestActor>)),
                new BehaviorStep(() => actor.Send(new TerminateMessage())),
            ],
        ]);

        actor.Start();
        actor.Join();
        Assert.True(ScenarioFlowTester.Complete);
        Mock.Get(ui)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_stateChanged")),
                Times.Once
            );
        Assert.True(called);
    }

    [Fact]
    public void PushStateAndReturnTest()
    {
        var ui = MockActorFactory.Create("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.TerminateWhenStateChanged(actor, typeof(StateE));
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            if (newState is StateD stateD)
                actor.Send(new Message(ui, "Return"));
            if (newState is StateC stateC)
                actor.Send(new Message(ui, "TransitToStateE"));
        };

        actor.State = new StateC(actor);
        actor.Start();
        actor.Send(new Message(ui, "LongSignal"));
        actor.Join();

        var mockActor = Mock.Get(ui);
        ActorUtils.VerifyGetMessage(
            actor,
            ui,
            "ReadLongSignalFromC",
            new ValueTuple<int, bool?>(0, null),
            Times.Once
        );
        ActorUtils.VerifyGetMessage(
            actor,
            ui,
            "ReadLongSignalFromC",
            new ValueTuple<int, bool?>(1, true),
            Times.Once
        );
        ActorUtils.VerifyGetMessage(actor, ui, "ReadLongSignalFromD", true, Times.Once);
        Assert.True(actor.GetStatus("LongSignal") is false);
    }

    [Fact]
    public void PushStateAndErrorTest()
    {
        var ui = MockActorFactory.Create("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.TerminateWhenStateChanged(actor, typeof(ErrorState<TestActor>));
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            if (newState is StateD stateD)
                actor.Send(new Message(ui, "GoError"));
        };

        actor.State = new StateC(actor);
        actor.Start();
        actor.Send(new Message(ui, "LongSignal"));
        actor.Join();

        ActorUtils.VerifyGetMessage(
            actor,
            ui,
            "ReadLongSignalFromC",
            new ValueTuple<int, bool?>(0, null),
            Times.Once
        );
        ActorUtils.VerifyGetMessage(actor, ui, "ReadLongSignalFromD", true, Times.Once);
        Assert.True(actor.GetStatus("LongSignal") is false);
    }

    private class TestActor : Actor
    {
        public TestActor(ActorConfig config)
            : base(config)
        {
            State = new StateA(this);
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<TestActor>(this, error);
        }
    }

    private class StateA(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "TransitToStateB":
                    Actor.State = new StateB(Actor);
                    return true;
                case "RaiseError":
                    throw new SequenceError();
            }

            return false;
        }
    }

    private class StateB(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                    break;
            }

            return false;
        }
    }

    private class StateC(TestActor actor) : State<TestActor>(actor)
    {
        private int _entryCount;

        public override void Dispose()
        {
            Actor.SetStatus("LongSignal", false);
        }

        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                {
                    var longSignal = Actor.GetStatus("LongSignal") as bool?;
                    Actor.Ui!.Send(
                        new Message(Actor, "ReadLongSignalFromC", (_entryCount, longSignal))
                    );
                    _entryCount++;
                    return true;
                }
                case "LongSignal":
                    Actor.SetStatus("LongSignal", true);
                    Actor.PushState(new StateD(Actor));
                    return true;
                case "TransitToStateE":
                    Actor.State = new StateE(Actor);
                    return true;
            }

            return false;
        }
    }

    private class StateD(TestActor actor) : State<TestActor>(actor)
    {
        public override void Dispose() { }

        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "Return":
                    Actor.Ui!.Send(
                        new Message(Actor, "ReadLongSignalFromD", Actor.GetStatus("LongSignal"))
                    );
                    if (!Actor.TryPopState())
                        Actor.State = new EmptyState();
                    return true;
                case "GoError":
                    Actor.Ui!.Send(
                        new Message(Actor, "ReadLongSignalFromD", Actor.GetStatus("LongSignal"))
                    );
                    throw new SequenceError();
            }

            return false;
        }
    }

    private class StateE(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            return false;
        }
    }
}
