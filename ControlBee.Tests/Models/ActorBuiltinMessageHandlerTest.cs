using System.Collections.Generic;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ActorBuiltinMessageHandler))]
public class ActorBuiltinMessageHandlerTest : ActorFactoryBase
{
    [Fact]
    public void InitializeAxisTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.State = new IdleState(actor);

        actor.Start();
        actor.Send(new Message(actor, "_initializeAxis", "X"));
        actor.Send(new Message(actor, "_terminate"));
        actor.Join();

        Assert.IsType<EmptyState>(actor.State);
    }

    [Fact]
    public void ResetStateTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.State = new IdleState(actor);

        actor.Start();
        actor.Send(new Message(actor, "_resetState"));
        actor.Send(new Message(actor, "_terminate"));
        actor.Join();

        Assert.IsType<EmptyState>(actor.State);
    }

    [Fact]
    public void DigestStatusTest()
    {
        var peer = Mock.Of<IActor>();
        Mock.Get(peer).Setup(m => m.Name).Returns("peer");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.InitPeers([peer]);
        Assert.Null(actor.PeerStatus[peer].GetValueOrDefault("Name"));

        actor.State = new IdleState(actor);
        actor.Start();
        actor.Send(
            new Message(peer, "_status", new Dictionary<string, object?> { ["Name"] = "Leo" })
        );
        actor.Send(new Message(actor, "_terminate"));
        actor.Join();
        Mock.Get(peer)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "ReplyYourName" && message.Payload as string == "Leo"
                        )
                    ),
                Times.Once
            );
    }

    public class TestActor : Actor
    {
        public Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([10.0]))
        );
        public Variable<SpeedProfile> HomeSpeedX = new();
        public IInitializeSequence InitializeSequenceX;
        public IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            InitializeSequenceX = config.InitializeSequenceFactory.Create(
                X,
                HomeSpeedX,
                HomePositionX
            );
            X.SetInitializeAction(() => throw new FatalSequenceError());
        }
    }

    public class IdleState(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "_status":
                {
                    var peer = Actor.PeerDict["peer"];
                    peer.Send(
                        new Message(
                            Actor,
                            "ReplyYourName",
                            Actor.PeerStatus[Actor.PeerDict["peer"]]["Name"]
                        )
                    );
                    return true;
                }
            }
            return false;
        }
    }

    [Fact]
    public void PropertyRead()
    {
        ActorItemInjectionDataSource.ReadFromString(
            @"
MyActor:
  Status:
    CenterDancer:
      Name: Start Centering Dancer
      Desc: Control dancer to be centered.
"
        );
        var client = MockActorFactory.Create("Client");
        var myActor = ActorFactory.Create<TestActor>("MyActor");

        myActor.Start();
        myActor.Send(new Message(client, "_propertyRead", "/Status/CenterDancer/Name"));
        myActor.Send(new Message(client, "_propertyRead", "Status/CenterDancer/Name"));
        myActor.Send(new Message(client, "_propertyRead", "Status/CenterDancer/Name/"));
        myActor.Send(new Message(client, "_propertyRead", "/Status/CenterDancer/Not-exist"));
        myActor.Send(new TerminateMessage());
        myActor.Join();

        Assert.Equal("Start Centering Dancer", myActor.GetProperty("/Status/CenterDancer/Name"));
        Assert.Equal("Start Centering Dancer", myActor.GetProperty("Status/CenterDancer/Name"));
        Assert.Equal("Start Centering Dancer", myActor.GetProperty("Status/CenterDancer/Name/"));
        Assert.Null(myActor.GetProperty("/Status/CenterDancer/Not-exist"));

        Mock.Get(client)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_property"
                            && message.Payload as string == "Start Centering Dancer"
                        )
                    ),
                Times.Exactly(3)
            );
        Mock.Get(client)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_property" && message.Payload == null
                        )
                    ),
                Times.Once
            );
    }
}
