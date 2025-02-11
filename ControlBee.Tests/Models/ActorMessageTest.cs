using System.Threading;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorMessageTest : ActorFactoryBase
{
    [Fact]
    public void TimeoutTest()
    {
        var actor = ActorFactory.Create<TestActorA1>("MyActor");
        actor.MessageFetchTimeout = 0;
        actor.Start();
        actor.Join();
    }

    public class TestActorA1(ActorConfig config) : Actor(config)
    {
        protected override bool ProcessMessage(Message message)
        {
            switch (message)
            {
                case TimeoutMessage:
                    Send(new TerminateMessage());
                    return true;
            }
            return base.ProcessMessage(message);
        }
    }
}
