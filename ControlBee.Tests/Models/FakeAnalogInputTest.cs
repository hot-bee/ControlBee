using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(FakeAnalogInput))]
public class FakeAnalogInputTest : ActorFactoryBase
{
    [Fact]
    public void ReadDataTest()
    {
        var input = new FakeAnalogInput { Data = 100 };
        Assert.Equal(100, input.Read());
    }

    [Fact]
    public void InjectPropertiesTest()
    {
        ActorItemInjectionDataSource.ReadFromString(
            @"
MyActor:
  MySensor:
    Name: My Sensor
    Desc: The description describing what my sensor is.
"
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Sensor", actor.MySensor.Name);
        Assert.Equal("The description describing what my sensor is.", actor.MySensor.Desc);
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/MySensor", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "ChangeValue"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MySensor" }
                && (long)payload["Data"]! == 0;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dictionary<string, object?>)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/MySensor" }
                && (long)payload["Data"]! == 100;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    public class TestActor(ActorConfig config) : Actor(config)
    {
        public IAnalogInput MySensor = new AnalogInputPlaceholder();

        protected override void MessageHandler(Message message)
        {
            switch (message.Name)
            {
                case "ChangeValue":
                    ((FakeAnalogInput)MySensor).Data = 100;
                    break;
            }

            base.MessageHandler(message);
        }
    }
}
