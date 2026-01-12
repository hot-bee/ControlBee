using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorMessageTest : ActorFactoryBase
{
    [Fact]
    public void TimeoutTest()
    {
        var actor = ActorFactory.Create<TestActorA1>("MyActor");
        actor.TimerMilliseconds = 0;
        actor.Start();
        actor.Join();
    }

    [Fact]
    public void SendMessageTest()
    {
        var listener = new ConcurrentQueue<string>();
        var actor1 = ActorFactory.Create<TestActorA>("MyActor1", listener);
        var actor2 = ActorFactory.Create<TestActorA>("MyActor2", listener);

        actor1.Start();
        actor2.Start();

        actor2.Send(new Message(actor1, "ping", 0));
        actor1.Join();
        actor2.Join();

        Assert.AreEqual(12, listener.Count);
        Assert.IsTrue(listener.Where((item, index) => index % 2 == 0).All(s => s == "MyActor2"));
        Assert.IsTrue(listener.Where((item, index) => index % 2 == 1).All(s => s == "MyActor1"));
    }

    [Fact]
    public void MessageProcessedTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");
        var stateTransitMatched = false;
        var stateEntryMessage = false;
        var retryWithEmptyMessageMatched = false;
        actor.MessageProcessed += (_, tuple) =>
        {
            if (
                tuple.oldState.GetType() == typeof(StateA)
                && tuple.newState.GetType() == typeof(StateB)
                && tuple.result
            )
                stateTransitMatched = true;
            if (
                tuple.oldState.GetType() == typeof(StateB)
                && tuple.message.GetType() == typeof(StateEntryMessage)
                && tuple.newState.GetType() == typeof(StateB)
                && tuple.result
            )
                stateEntryMessage = true;
            if (
                tuple.oldState.GetType() == typeof(StateB)
                && tuple.message == Message.Empty
                && tuple.newState.GetType() == typeof(StateB)
                && !tuple.result
            )
                retryWithEmptyMessageMatched = true;
        };
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Assert.IsTrue(stateTransitMatched);
        Assert.IsTrue(stateEntryMessage);
        Assert.IsFalse(retryWithEmptyMessageMatched);
    }

    [Fact]
    public void StateChangeTest()
    {
        var uiActor = MockActorFactory.Create("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.IsInstanceOfType<StateB>(actor.State);
        Mock.Get(uiActor)
            .Verify(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_stateChanged" && message.Payload as string == "StateB"
                    )
                )
            );
    }

    [Fact]
    public void OverrideProcessMessageTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "foo2"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.IsInstanceOfType<StateA>(actor.State);
    }

    [Fact(Skip = "We don't care the return value any more.")]
    public void WrongProcessMessageReturnTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "qoo"));
        actor.Join();
        Assert.IsNotNull(actor.ExitError);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    public void DroppedMessageTest(string messageName)
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");
        var sender = Mock.Of<IActor>();

        actor.Start();
        var myMessage = new Message(sender, messageName);
        actor.Send(myMessage);
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        if (messageName == "foo")
            Mock.Get(sender).Verify(m => m.Send(It.IsAny<DroppedMessage>()), Times.Never);
        else
            Mock.Get(sender)
                .Verify(
                    m =>
                        m.Send(It.Is<DroppedMessage>(message => message.RequestId == myMessage.Id)),
                    Times.Once
                );
    }

    [Fact]
    public void NoInfiniteDroppedMessageTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");
        var sender = Mock.Of<IActor>();

        actor.Start();
        actor.Send(new DroppedMessage(Guid.Empty, sender));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Mock.Get(sender).Verify(m => m.Send(It.IsAny<DroppedMessage>()), Times.Never);
    }

    [Fact]
    public void StateEntryMessageWhenStartTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");
        var state = new StateA(actor);
        actor.State = state;
        Assert.IsFalse(state.Started);

        actor.Start();
        actor.Send(new TerminateMessage());
        actor.Join();
        Assert.IsTrue(state.Started);
    }

    private class TestActorA1(ActorConfig config) : Actor(config)
    {
        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case TimerMessage.MessageName:
                    Send(new TerminateMessage());
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }

    private class TestActorA(ActorConfig config, ConcurrentQueue<string> listener) : Actor(config)
    {
        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "ping":
                {
                    var count = (int)message.Payload! + 1;
                    if (count > 12)
                    {
                        message.Sender.Send(new TerminateMessage());
                        Send(new TerminateMessage());
                        return true;
                    }

                    listener.Enqueue(Name);
                    message.Sender.Send(new Message(this, "ping", count));
                    return true;
                }
            }

            return false;
        }
    }

    private class TestActorB : Actor
    {
        public TestActorB(ActorConfig config)
            : base(config)
        {
            State = new StateA(this);
        }
    }

    private class StateA(TestActorB actor) : State<TestActorB>(actor)
    {
        public bool Started;

        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                    Started = true;
                    return true;
                case "foo":
                    Actor.State = new StateB(Actor);
                    return true;
                case "qoo":
                    Actor.State = new StateB(Actor);
                    return false; // Intended
                default:
                    return false;
            }
        }
    }

    private class StateB(TestActorB actor) : State<TestActorB>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                    return true;
                case "foo2":
                    Actor.State = new StateA(Actor);
                    return true;
            }

            return false;
        }
    }
}
