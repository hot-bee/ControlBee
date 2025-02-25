using System.Configuration;
using ControlBee.Exceptions;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
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

        ScenarioFlowTester.Setup(
            [
                [
                    new BehaviorStep(
                        () => actor.Send(new Message(EmptyActor.Instance, "TransitToStateB"))
                    ),
                    new ConditionStep(() => actor.State.GetType() == typeof(StateB)),
                    new BehaviorStep(() => actor.Send(new TerminateMessage())),
                ],
            ]
        );

        actor.Start();
        actor.Join();
        Assert.True(ScenarioFlowTester.Complete);
        Assert.True(called);
    }

    [Fact]
    public void StateChangeWithErrorTest()
    {
        var ui = MockActorFactory.Create("ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        var called = false;
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            Assert.IsType<StateA>(oldState);
            Assert.IsType<ErrorState>(newState);
            called = true;
        };
        ScenarioFlowTester.Setup(
            [
                [
                    new BehaviorStep(
                        () => actor.Send(new Message(EmptyActor.Instance, "RaiseError"))
                    ),
                    new ConditionStep(() => actor.State.GetType() == typeof(ErrorState)),
                    new BehaviorStep(() => actor.Send(new TerminateMessage())),
                ],
            ]
        );

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

    private class TestActor : Actor
    {
        public TestActor(ActorConfig config)
            : base(config)
        {
            State = new StateA(this);
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
}
