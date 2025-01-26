using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeDigitalOutput))]
public class FakeDigitalOutputTest : ActorFactoryBase
{
    [Fact]
    public void OnOffTest()
    {
        var fakeDigitalOutput = new FakeDigitalOutput();
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = true;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);

        fakeDigitalOutput.On = false;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = true;
        Assert.False(fakeDigitalOutput.On);
        Assert.True(fakeDigitalOutput.Off);

        fakeDigitalOutput.Off = false;
        Assert.True(fakeDigitalOutput.On);
        Assert.False(fakeDigitalOutput.Off);
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
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)payload["On"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
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
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)actorItemMessage.DictPayload!["On"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Vacuum" }
                && (bool)actorItemMessage.DictPayload!["On"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    public class TestActor : Actor
    {
        public IDigitalOutput Vacuum;

        public TestActor(ActorConfig config)
            : base(config)
        {
            Vacuum = DigitalOutputFactory.Create();
        }

        protected override void ProcessMessage(Message message)
        {
            if (message.Name == "Go")
            {
                Vacuum.On = true;
                return;
            }

            base.ProcessMessage(message);
        }
    }
}
