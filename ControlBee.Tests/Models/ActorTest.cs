using System.Collections.Generic;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Utils;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorTest : ActorFactoryBase
{
    [Fact]
    public void TitleTest()
    {
        var actor = ActorFactory.Create<Actor>("myActor");
        actor.Title.Should().Be("myActor");
        actor.SetTitle("MyActor");
        actor.Title.Should().Be("MyActor");
    }

    [Fact]
    public void DisposeActorWithoutStartingTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.Dispose();
    }

    [Fact]
    public void ActorLifeTest()
    {
        var timeManager = Mock.Of<ITimeManager>();
        Recreate(new ActorFactoryBaseConfig { TimeManager = timeManager });

        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Mock.Get(timeManager).Verify(m => m.Register(), Times.Once);
        Mock.Get(timeManager).Verify(m => m.Unregister(), Times.Once);
    }

    [Fact]
    public void ProcessMessageTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var variable = Mock.Of<IVariable>();
        Mock.Get(variable).Setup(m => m.Actor).Returns(actor);
        actor.AddItem(variable, "/myVar");

        actor.Start();
        actor.Send(new ActorItemMessage(EmptyActor.Instance, "myVar", "hello"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Mock.Get(variable).Verify(m => m.ProcessMessage(It.IsAny<ActorItemMessage>()), Times.Once);
    }

    [Fact]
    public void GetItemsTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        var myVariable = new Variable<int>(VariableScope.Global, 1);
        actor.AddItem(myVariable, "/MyVar");
        var myDigitalOutput = new FakeDigitalOutput(DeviceManager, TimeManager);
        actor.AddItem(myDigitalOutput, "/MyOutput");

        var items = actor.GetItems();

        Assert.Equal(2, items.Length);
        Assert.Equal("/MyVar", items[0].itemPath);
        Assert.True(items[0].type.IsAssignableTo(typeof(IVariable)));
        Assert.Equal("/MyOutput", items[1].itemPath);
        Assert.True(items[1].type.IsAssignableTo(typeof(IDigitalOutput)));
    }

    [Fact]
    public void GetItemTest()
    {
        var actor = ActorFactory.Create<TestActorC>("MyActor");
        Assert.Equal(actor.X, actor.GetItem("X"));
        Assert.Equal(actor.X, actor.GetItem("/X"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipWaitSensorTest(bool skipWaitSensor)
    {
        Recreate(
            new ActorFactoryBaseConfig
            {
                SystemConfigurations = new SystemConfigurations
                {
                    FakeMode = true,
                    SkipWaitSensor = skipWaitSensor,
                },
            }
        );
        var actor = ActorFactory.Create<Actor>("MyActor");
        Assert.Equal(skipWaitSensor, actor.SkipWaitSensor);
    }

    [Fact]
    public void UpdateTitle()
    {
        ActorItemInjectionDataSource.ReadFromString(
            """

            MyActor:
              Name: RealName

            """
        );
        var myActor = ActorFactory.Create<TestActorC>("MyActor");

        myActor.Start();
        myActor.Send(new TerminateMessage());
        myActor.Join();

        Assert.Equal("RealName", myActor.Title);
    }

    [Fact]
    public void ErrorStateTest()
    {
        var actor = ActorFactory.Create<TestActorC>("MyActor");
        actor.State = new StateE(actor);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "DoSomethingWrong"));
        actor.Send(new TerminateMessage());
        actor.Join();
        Assert.IsType<ErrorState>(actor.State);
    }

    [Fact]
    public void FatalErrorStateTest()
    {
        var actor = ActorFactory.Create<TestActorC>("MyActor");
        actor.State = new StateE(actor);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "DoSomethingTotallyWrong"));
        actor.Send(new TerminateMessage());
        actor.Join();
        Assert.IsType<EmptyState>(actor.State);
    }

    [Fact]
    public void StatusTest()
    {
        var ui = MockActorFactory.Create("ui");
        ActorRegistry.Add(ui);
        //var peer = MockActorFactory.Create("Peer");
        var peer = ActorFactory.Create<Actor>("Peer");
        var actor = ActorFactory.Create<Actor>("MyActor");

        peer.InitPeers([actor]);
        actor.InitPeers([peer]);

        peer.State = new StateD(peer);
        actor.State = new StateC(actor);

        peer.Start();
        actor.Start();
        actor.Send(new Message(peer, "_status", new Dict { ["Name"] = "Biden" }));
        actor.Send(new TerminateMessage());

        peer.Join();
        actor.Join();

        Assert.Equal("Trump", actor.GetStatus("Name"));
        Assert.Null(actor.GetStatus("Phone"));
        Mock.Get(ui)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && message.DictPayload!["Name"] as string == "Leo"
                        )
                    ),
                Times.Once
            );
        Mock.Get(ui)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && message.DictPayload!["Name"] as string == "Trump"
                        )
                    ),
                Times.AtLeastOnce
            );
        Mock.Get(ui)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && DictPath.Start(message.DictPayload)["Peer"]["WifiId"].Value as string
                                == "KTWorld"
                        )
                    ),
                Times.AtLeastOnce
            );
        Mock.Get(ui)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "GotYourName" && message.Payload as string == "Biden"
                        )
                    ),
                Times.Once
            );
        Mock.Get(ui)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "GotWifiId" && message.Payload as string == "KTWorld"
                        )
                    ),
                Times.Once
            );
    }

    public class TestActorC : Actor
    {
        public IAxis X;

        public TestActorC(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
        }
    }

    public class StateC(Actor actor) : State<Actor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case StateEntryMessage.MessageName:
                    Actor.SetStatus("Name", "Leo");
                    Actor.SetStatus("Name", "Trump");
                    Actor.SetStatusByActor("Peer", "WifiId", "KTWorld");
                    return true;
                case "_status":
                {
                    var peerName = Actor.GetPeerStatus("Peer", "Name");
                    if (peerName != null)
                        Actor.PeerDict["ui"].Send(new Message(Actor, "GotYourName", peerName));
                    return true;
                }
            }

            return false;
        }
    }

    public class StateD(Actor actor) : State<Actor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "_status":
                {
                    var wifiId = Actor.GetPeerStatusByActor("MyActor", "WifiId");
                    if (wifiId != null)
                    {
                        Actor.PeerDict["ui"].Send(new Message(Actor, "GotWifiId", wifiId));
                        Actor.Send(new TerminateMessage());
                    }

                    return true;
                }
            }

            return false;
        }
    }

    public class StateE(Actor actor) : State<Actor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "DoSomethingWrong":
                    throw new SequenceError();
                case "DoSomethingTotallyWrong":
                    throw new FatalSequenceError();
            }

            return false;
        }
    }
}
