using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalOutput))]
public class FakeDigitalOutputTest : ActorFactoryBase
{
    [Fact]
    public void OnOffTest()
    {
        var fakeDigitalOutput = new FakeDigitalOutput(DeviceManager, TimeManager);
        Assert.Null(fakeDigitalOutput.IsOn());
        Assert.Null(fakeDigitalOutput.IsOff());

        fakeDigitalOutput.On();
        Assert.True(fakeDigitalOutput.IsCommandOn());
        Assert.True(fakeDigitalOutput.IsCommandOff() == false);
        Assert.True(fakeDigitalOutput.IsOn() is null);
        Assert.True(fakeDigitalOutput.IsOff() is null);

        fakeDigitalOutput.Off();
        Assert.True(fakeDigitalOutput.IsCommandOn() == false);
        Assert.True(fakeDigitalOutput.IsCommandOff());
        Assert.True(fakeDigitalOutput.IsOn() is null);
        Assert.True(fakeDigitalOutput.IsOff() is null);
    }

    [Fact]
    public void OnAndWaitTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "OnAndWait"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(TimeManager.CurrentMilliseconds is >= 100 and < 1000);
        Assert.True(actor.Vacuum.IsOn() is true);
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/Vacuum", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "OnAndWait"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]!
                && payload["IsOn"] == null;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);

        var match3 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]!
                && payload["IsOn"] is true;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match3(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/Vacuum", "_itemDataRead"));
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Vacuum",
                "_itemDataWrite",
                new Dictionary<string, object?> { ["On"] = true }
            )
        );
        actor.Send(new Message(uiActor, "Wait"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && actorItemMessage.DictPayload!["On"] is false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && actorItemMessage.DictPayload!["On"] is true
                && actorItemMessage.DictPayload!["IsOn"] is null;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);

        var match3 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && actorItemMessage.DictPayload!["On"] is true
                && actorItemMessage.DictPayload!["IsOn"] is true;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match3(message))), Times.Once);
    }

    public class TestActor : Actor
    {
        public IDigitalOutput Vacuum;

        public TestActor(ActorConfig config)
            : base(config)
        {
            Vacuum = config.DigitalOutputFactory.Create();
        }

        protected override void MessageHandler(Message message)
        {
            switch (message.Name)
            {
                case "Wait":
                    Vacuum.Wait();
                    return;
                case "OnAndWait":
                    Vacuum.OnAndWait();
                    return;
                default:
                    base.MessageHandler(message);
                    break;
            }
        }
    }
}
