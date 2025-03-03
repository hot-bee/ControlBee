using System;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using DeviceBase;
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
        SystemPropertiesDataSource.ReadFromString(
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
        SetupWithDevice();

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
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Exactly(2));

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
        SetupWithDevice();

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
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Exactly(2));

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

    [Fact]
    public void InitializeTest()
    {
        SetupWithDevice();

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
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.Exactly(2));
    }

    [Fact]
    public void GetStepJogSizesTest()
    {
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        var called = false;
        ActorUtils.SetupActionOnGetMessage(
            client,
            "_stepJogSizes",
            message =>
            {
                Assert.Equal([0.01, 0.1, 10.0], (double[])message.Payload!);
                called = true;
            }
        );

        actor.Start();
        actor.Send(new ActorItemMessage(client, "/X", "_getStepJogSizes"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.True(called);
    }

    [Fact]
    public void JogTest()
    {
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        ActorUtils.SetupActionOnGetMessage(
            client,
            "_jogStarted",
            message =>
            {
                Assert.True(actor.X.IsMoving());
                actor.Send(new ActorItemMessage(client, "/X", "_jogStop"));
            }
        );
        ActorUtils.SetupActionOnGetMessage(
            client,
            "_jogStopped",
            message =>
            {
                Assert.False(actor.X.IsMoving());
                actor.Send(new TerminateMessage());
            }
        );

        actor.Start();
        actor.Send(
            new ActorItemMessage(
                client,
                "/X",
                "_jogStart",
                new Dict { ["Direction"] = AxisDirection.Positive, ["JogSpeed"] = JogSpeed.Medium }
            )
        );
        actor.Join();
    }

    private IMotionDevice SetupWithDevice()
    {
        SystemPropertiesDataSource.ReadFromString(
            """
              MyActor:
                X:
                  DeviceName: MyDevice
                  Channel: 0
            """
        );

        var device = Mock.Of<IMotionDevice>();
        DeviceManager.Add("MyDevice", device);
        return device;
    }

    [Fact]
    public void EnableTest()
    {
        Recreate(new ActorFactoryBaseConfig { SystemConfigurations = new SystemConfigurations() });
        var device = SetupWithDevice();

        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(client, "EnableX"));
        actor.Send(new TerminateMessage());
        actor.Join();

        Assert.Equal(200, TimeManager.CurrentMilliseconds);
        Mock.Get(device).Verify(m => m.Enable(0, true));
    }

    private class TestActor : Actor
    {
        public readonly IAxis X;
        public bool Initialized;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            X.SetInitializeAction(() => Initialized = true);

            ((Axis)X).StepJogSizes.Value[2] = 10;
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                    X.SetPosition(100.0);
                    return true;
                case "ChangeValue":
                    X.SetPosition(200.0);
                    return true;
                case "EnableX":
                    X.Enable();
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }
}
