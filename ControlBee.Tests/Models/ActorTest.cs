using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorTest : ActorFactoryBase
{
    [Fact]
    public void SendMessageTest()
    {
        var listener = new ConcurrentQueue<string>();
        var actor1 = ActorFactory.Create<TestActorA>("MyActor1", "foo", listener);
        var actor2 = ActorFactory.Create<TestActorA>("MyActor2", "bar", listener);

        actor1.Start();
        actor2.Start();

        actor2.Send(new Message(actor1, "foo"));
        actor1.Join();
        actor2.Join();

        Assert.Equal(12, listener.Count);
        Assert.True(listener.Where((item, index) => index % 2 == 0).All(s => s == "foo"));
        Assert.True(listener.Where((item, index) => index % 2 == 1).All(s => s == "bar"));
    }

    [Fact]
    public void TitleTest()
    {
        var actor = ActorFactory.Create<Actor>("myActor");
        actor.Title.Should().Be("myActor");
        actor.SetTitle("MyActor");
        actor.Title.Should().Be("MyActor");
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
                && tuple.message.GetType() == typeof(OnStateEntryMessage)
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
        stateTransitMatched.Should().BeTrue();
        Assert.True(stateEntryMessage);
        retryWithEmptyMessageMatched.Should().BeTrue();
    }

    [Fact]
    public void StateChangeTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.IsType<StateB>(actor.State);
    }

    [Fact]
    public void OverrideProcessMessageTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "reverseFoo"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.IsType<StateA>(actor.State);
    }

    [Fact]
    public void WrongProcessMessageReturnTest()
    {
        var actor = ActorFactory.Create<TestActorB>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "qoo"));
        actor.Join();
        Assert.NotNull(actor.ExitError);
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
    public void DisposeActorWithoutStartingTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.Dispose();
    }

    [Fact]
    public void ActorLifeTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        Recreate(new ActorFactoryBaseConfig { TimeManager = timeManager });

        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Mock.Get(timeManager).Verify(m => m.Register(), Times.Once);
        Mock.Get(timeManager).Verify(m => m.Unregister(), Times.Once);
    }

    [Fact]
    public void ProcessMessageTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = Mock.Of<IVariable>();
        Mock.Get(variable).Setup(m => m.Actor).Returns(actor);
        actor.AddItem(variable, "/myVar");

        actor.Start();
        actor.Send(new ActorItemMessage(EmptyActor.Instance, "myVar", "hello"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Mock.Get(variable).Verify(m => m.ProcessMessage(It.IsAny<ActorItemMessage>()), Times.Once);
    }

    [Fact]
    public void GetItemsTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var myVariable = new Variable<int>(VariableScope.Global, 1);
        actor.AddItem(myVariable, "/MyVar");
        var myDigitalOutput = new FakeDigitalOutput(DeviceManager, TimeManager);
        actor.AddItem(myDigitalOutput, "/MyOutput");

        var items = actor.GetItems();

        Assert.Equal(2, items.Length);
        Assert.Equal("/MyVar", items[0].itemPath);
        Assert.True(items[0].type.IsAssignableTo(typeof(IVariable)));
        Assert.Equal("/MyOutput", items[1].itemPath);
        Assert.True(items[1].type.IsAssignableTo(typeof(IDigitalOutput)));
    }

    [Fact]
    public void GetItemTest()
    {
        var actor = ActorFactory.Create<TestActorC>("MyActor");
        Assert.Equal(actor.X, actor.GetItem("X"));
        Assert.Equal(actor.X, actor.GetItem("/X"));
    }

    public class TestActorA(
        ActorConfig config,
        string messageName,
        ConcurrentQueue<string> listener
    ) : Actor(config)
    {
        protected override void MessageHandler(Message message)
        {
            listener.Enqueue(message.Name);
            message.Sender.Send(new Message(this, messageName));
            if (listener.Count > 10)
                throw new OperationCanceledException();
        }
    }

    public class TestActorB : Actor
    {
        public TestActorB(ActorConfig config)
            : base(config)
        {
            State = new StateA(this);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "reverseFoo":
                    State = new StateA(this);
                    return true;
            }

            return false;
        }
    }

    public class StateA(TestActorB actor) : State<TestActorB>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            if (message.Name == "foo")
            {
                Actor.State = new StateB(Actor);
                return true;
            }

            if (message.Name == "qoo")
            {
                Actor.State = new StateB(Actor);
                return false;
            }

            return false;
        }
    }

    public class StateB(TestActorB actor) : State<TestActorB>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case OnStateEntryMessage.MessageName:
                    return true;
            }

            return false;
        }
    }

    public class TestActorC : Actor
    {
        public IAxis X;

        public TestActorC(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
        }
    }
}
