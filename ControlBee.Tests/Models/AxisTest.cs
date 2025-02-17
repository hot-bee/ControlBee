﻿using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Axis))]
public class AxisTest : ActorFactoryBase
{
    [Fact]
    public void InjectPropertiesTest()
    {
        ActorItemInjectionDataSource.ReadFromString(
            """

            MyActor:
              X:
                Name: X-axis
                Desc: My precious X axis.

            """
        );
        var actor = ActorFactory.Create<TestActor>("MyActor");

        Assert.Equal("X-axis", actor.X.Name);
        Assert.Equal("My precious X axis.", actor.X.Desc);
    }

    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/X", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "ChangeValue"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                && (double)payload["CommandPosition"]! == 100.0;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            var payload = (Dict)actorItemMessage.Payload!;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                && (double)payload["CommandPosition"]! == 200.0;
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
        actor.Send(new ActorItemMessage(uiActor, "/X", "_itemDataRead"));
        actor.Send(
            new ActorItemMessage(uiActor, "/X", "_itemDataWrite", new Dict { ["Enable"] = true })
        );
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && (bool)actorItemMessage.DictPayload!["IsEnabled"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && (bool)actorItemMessage.DictPayload!["IsEnabled"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }

    public class TestActor : Actor
    {
        public IAxis X;
        public bool Initialized = false;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            X.SetPosition(100.0);
            X.SetInitializeAction(() => Initialized = true);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "ChangeValue":
                    X.SetPosition(200.0);
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }

    [Fact]
    public void InitializeTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/X", "_initialize"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Initialized);

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && (bool)actorItemMessage.DictPayload!["IsInitializing"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/X" }
                && (bool)actorItemMessage.DictPayload!["IsInitializing"]! == false;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Once);
    }
}
