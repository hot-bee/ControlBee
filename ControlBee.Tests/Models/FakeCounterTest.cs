using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeCounter))]
public class FakeCounterTest : ActorFactoryBase
{
    [Fact]
    public void GetSetTest()
    {
        var counter = new FakeCounter();
        Assert.Equal(0, counter.GetCounterValue());

        counter.SetCounterValue(42);
        Assert.Equal(42, counter.GetCounterValue());

        counter.Count = 100;
        Assert.Equal(100, counter.GetCounterValue());
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/MyCounter", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "SetCounterValue"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyCounter" }
                && (double)message.DictPayload!["Count"]! == 0;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyCounter" }
                && (double)message.DictPayload!["Count"]! == 77;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    public class TestActor(ActorConfig config) : Actor(config)
    {
        public readonly ICounter MyCounter = new CounterPlaceholder();

        protected override void MessageHandler(Message message)
        {
            switch (message.Name)
            {
                case "SetCounterValue":
                    ((FakeCounter)MyCounter).SetCounterValue(77);
                    break;
            }

            base.MessageHandler(message);
        }
    }
}
