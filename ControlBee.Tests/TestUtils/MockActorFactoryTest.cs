using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.TestUtils;

[TestSubject(typeof(MockActorFactory))]
public class MockActorFactoryTest : ActorFactoryBase
{
    [Fact]
    public void MockActorTest()
    {
        var caller = Mock.Of<IActor>();
        var actor = MockActorFactory.Create<TestActor>("MyActor");
        Mock.Get(actor).Setup(m => m.Foo()).Returns("bar");

        actor.Start();
        actor.Send(new Message(caller, "foo"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Mock.Get(caller).Verify(m => m.Send(It.Is<Message>(message => message.Name == "bar")));
    }

    public class TestActor : Actor
    {
        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "foo":
                    message.Sender.Send(new Message(this, Foo()));
                    return true;
            }

            return false;
        }

        public virtual string Foo()
        {
            return "foo";
        }
    }
}
