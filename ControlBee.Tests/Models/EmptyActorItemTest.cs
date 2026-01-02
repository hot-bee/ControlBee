using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(EmptyActorItem))]
public class EmptyActorItemTest : ActorFactoryBase
{
    [Fact]
    public void GetMetaDataTest()
    {
        SystemPropertiesDataSource.ReadFromString(
            @"
MyActor:
  EmptyItem:
    Name: My Empty Item
    Desc: The description describing what my empty item is.
"
        );
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.Equal("My Empty Item", actor.EmptyItem.Name);
        Assert.Equal("The description describing what my empty item is.", actor.EmptyItem.Desc);

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/EmptyItem", "_itemMetaDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
        {
            var metaData = message.DictPayload;
            return message.Name == "_itemMetaData"
                && metaData!["Name"] as string == "My Empty Item"
                && metaData["Desc"] as string
                    == "The description describing what my empty item is.";
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    private class TestActor(ActorConfig config) : Actor(config)
    {
        public readonly EmptyActorItem EmptyItem = new();
    }
}
