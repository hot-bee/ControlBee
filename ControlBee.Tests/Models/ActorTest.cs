using System;
using System.Collections.Concurrent;
using System.Linq;
using ControlBee.Models;
using JetBrains.Annotations;
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
            (self, message) =>
            {
                listener.Enqueue((string)message.Payload);
                message.Sender.Send(new Message(self, "foo"));
                if (listener.Count > 10)
                    throw new OperationCanceledException();
            }
        );

        var actor2 = new Actor(
            (self, message) =>
            {
                listener.Enqueue((string)message.Payload);
                message.Sender.Send(new Message(self, "bar"));
                if (listener.Count > 10)
                    throw new OperationCanceledException();
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
}