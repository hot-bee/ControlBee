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

[TestSubject(typeof(FakeAnalogOutput))]
public class FakeAnalogOutputTest : ActorFactoryBase
{
    [Fact]
    public void WriteDataTest()
    {
        var fakeAnalogOutput = new FakeAnalogOutput();

        Assert.Equal(0, fakeAnalogOutput.Read());
        fakeAnalogOutput.Write(100);
        Assert.Equal(100, fakeAnalogOutput.Read());
    }

    [Fact]
    public void InjectPropertiesTest()
    {
        SystemPropertiesDataSource.ReadFromString(
            """
            MyActor:
              MyActuator:
                Name: My Sensor
                Desc: The description describing what my sensor is.
            """
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Sensor", actor.MyActuator.Name);
        Assert.Equal("The description describing what my sensor is.", actor.MyActuator.Desc);
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/MyActuator", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "ChangeValue"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyActuator" }
                && (long)payload["Data"]! == 0;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyActuator" }
                && (long)payload["Data"]! == 100;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/MyActuator", "_itemDataRead"));
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/MyActuator",
                "_itemDataWrite",
                new Dict { ["Data"] = 200 }
            )
        );
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyActuator" }
                && (long)actorItemMessage.DictPayload!["Data"]! == 0;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MyActuator" }
                && (long)actorItemMessage.DictPayload!["Data"]! == 200;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    private class TestActor(ActorConfig config) : Actor(config)
    {
        public IAnalogOutput MyActuator = new AnalogOutputPlaceholder();

        protected override void MessageHandler(Message message)
        {
            switch (message.Name)
            {
                case "ChangeValue":
                    ((FakeAnalogOutput)MyActuator).Write(100);
                    break;
            }

            base.MessageHandler(message);
        }
    }
}
