using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
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
        var retryWithEmptyMessageMatched = false;
        actor.MessageProcessed += (_, tuple) =>
        {
            if (
                tuple.oldState.GetType() == typeof(StateA)
                && tuple.newState.GetType() == typeof(StateB)
            )
                stateTransitMatched = true;
            if (
                tuple.oldState.GetType() == typeof(StateB)
                && tuple.message == Message.Empty
                && tuple.newState.GetType() == typeof(StateB)
            )
                retryWithEmptyMessageMatched = true;
        };
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "foo"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        stateTransitMatched.Should().BeTrue();
        retryWithEmptyMessageMatched.Should().BeTrue();
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
        ActorFactory = new ActorFactory(
            SystemConfigurations,
            AxisFactory,
            DigitalInputFactory,
            DigitalOutputFactory,
            InitializeSequenceFactory,
            VariableManager,
            timeManager,
            ScenarioFlowTester,
            ActorItemInjectionDataSource,
            ActorRegistry
        );
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
        protected override void ProcessMessage(Message message)
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
            State = new StateA();
        }
    }

    public class StateA : IState
    {
        public IState ProcessMessage(Message message)
        {
            if (message.Name == "foo")
                return new StateB();
            return this;
        }
    }

    public class StateB : IState
    {
        public IState ProcessMessage(Message message)
        {
            return this;
        }
    }

    public class TestActorC : Actor
    {
        public IAxis X;

        public TestActorC(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
        }
    }
}
