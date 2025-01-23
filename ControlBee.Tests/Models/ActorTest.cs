using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorTest
{
    [Fact]
    public void SendMessageTest()
    {
        var listener = new ConcurrentQueue<string>();
        var actor1 = new Actor(
            (self, state, message) =>
            {
                listener.Enqueue(message.Name);
                message.Sender.Send(new Message(self, "foo"));
                if (listener.Count > 10)
                    throw new OperationCanceledException();
                return state;
            }
        );

        var actor2 = new Actor(
            (self, state, message) =>
            {
                listener.Enqueue(message.Name);
                message.Sender.Send(new Message(self, "bar"));
                if (listener.Count > 10)
                    throw new OperationCanceledException();
                return state;
            }
        );
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
        var actor = new Actor("myActor");
        actor.Title.Should().Be("myActor");
        actor.SetTitle("MyActor");
        actor.Title.Should().Be("MyActor");
    }

    [Fact]
    public void MessageProcessedTest()
    {
        var oldState = Mock.Of<IState>();
        var newState = Mock.Of<IState>();
        var actor = new Actor(
            (_, state, message) =>
            {
                if (state == oldState && message.Name == "foo")
                    return newState;
                return state;
            }
        );
        actor.State = oldState;
        var stateTransitMatched = false;
        var retryWithEmptyMessageMatched = false;
        actor.MessageProcessed += (_, tuple) =>
        {
            if (tuple.oldState == oldState && tuple.newState == newState)
                stateTransitMatched = true;
            if (
                tuple.oldState == newState
                && tuple.message == Message.Empty
                && tuple.newState == newState
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
        var actor = new Actor("myActor");
        actor.Dispose();
    }

    [Fact]
    public void ActorLifeTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                EmptyVariableManager.Instance,
                timeManager,
                EmptyActorItemInjectionDataSource.Instance
            )
        );
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Mock.Get(timeManager).Verify(m => m.Register(), Times.Once);
        Mock.Get(timeManager).Verify(m => m.Unregister(), Times.Once);
    }

    [Fact]
    public void ProcessMessageTest()
    {
        var database = Mock.Of<IDatabase>();
        var variableManager = new VariableManager(database);
        var actor = new Actor("myActor");
        var variable = Mock.Of<IVariable>();
        Mock.Get(variable).Setup(m => m.Actor).Returns(actor);
        actor.AddItem(variable, "/myVar");
        variableManager.Add(variable);

        actor.Start();
        actor.Send(new ActorItemMessage(EmptyActor.Instance, "myVar", "hello"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Mock.Get(variable).Verify(m => m.ProcessMessage(It.IsAny<ActorItemMessage>()), Times.Once);
    }

    [Fact]
    public void GetItemsTest()
    {
        var actor = new Actor(
            new ActorConfig(
                "myActor",
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                EmptyVariableManager.Instance,
                EmptyTimeManager.Instance,
                EmptyActorItemInjectionDataSource.Instance
            )
        );
        var myVariable = new Variable<int>(actor, "/MyVar", VariableScope.Global, 1);
        actor.AddItem(myVariable, "/MyVar");
        var myDigitalOutput = new FakeDigitalOutput();
        actor.AddItem(myDigitalOutput, "/MyOutput");

        var items = actor.GetItems();

        Assert.Equal(2, items.Length);
        Assert.Equal("/MyVar", items[0].itemPath);
        Assert.True(items[0].type.IsAssignableTo(typeof(IVariable)));
        Assert.Equal("/MyOutput", items[1].itemPath);
        Assert.True(items[1].type.IsAssignableTo(typeof(IDigitalOutput)));
    }

    public class TestActor : Actor
    {
        public TestActor(ActorConfig config)
            : base(config) { }
    }
}
