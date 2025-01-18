using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
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
            (self, state, message) =>
            {
                if (state == oldState && message.Name == "foo")
                    return newState;
                return state;
            }
        );
        actor.State = oldState;
        var stateTransitMatched = false;
        var RetryWithEmptyMessageMatched = false;
        actor.MessageProcessed += (sender, tuple) =>
        {
            if (tuple.oldState == oldState && tuple.newState == newState)
                stateTransitMatched = true;
            if (
                tuple.oldState == newState
                && tuple.message == Message.Empty
                && tuple.newState == newState
            )
                RetryWithEmptyMessageMatched = true;
        };
        actor.Start();
        actor.Send(new Message(Actor.Empty, "foo"));
        actor.Send(new Message(Actor.Empty, "_terminate"));
        actor.Join();
        stateTransitMatched.Should().BeTrue();
        RetryWithEmptyMessageMatched.Should().BeTrue();
    }
}
