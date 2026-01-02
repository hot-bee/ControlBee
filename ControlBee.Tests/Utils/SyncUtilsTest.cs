using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Utils;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Utils;

[TestSubject(typeof(SyncUtils))]
public class SyncUtilsTest : ActorFactoryBase
{
    [Fact]
    public void NormalTest()
    {
        var peer1 = MockActorFactory.Create("Peer1");
        var peer2 = MockActorFactory.Create("Peer2");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeer(peer1, peer2);

        actor.Start();
        ActorUtils.SendSignalByActor(peer1, actor, "ReadyToDo", Guid.NewGuid());
        ActorUtils.SendSignalByActor(peer2, actor, "ReadyToDo", Guid.NewGuid());
        ActorUtils.SendSignalByActor(peer1, actor, "ReadyToDo", Guid.NewGuid());

        actor.Send(new TerminateMessage());
        actor.Join();

        ActorUtils.VerifyGetMessage(actor, peer1, "Do", Times.Once);
        ActorUtils.VerifyGetMessage(actor, peer2, "Do", Times.Once);
    }

    [Fact]
    public void GrantTwiceTest()
    {
        var peer1 = MockActorFactory.Create("Peer1");
        var peer2 = MockActorFactory.Create("Peer2");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeer(peer1, peer2);

        actor.Start();
        ActorUtils.SendSignalByActor(peer1, actor, "ReadyToDo", Guid.NewGuid());
        ActorUtils.SendSignalByActor(peer2, actor, "ReadyToDo", Guid.NewGuid());
        ActorUtils.SendSignalByActor(peer1, actor, "ReadyToDo", Guid.NewGuid());
        ActorUtils.SendSignalByActor(peer2, actor, "ReadyToDo", Guid.NewGuid());

        actor.Send(new TerminateMessage());
        actor.Join();

        ActorUtils.VerifyGetMessage(actor, peer1, "Do", () => Times.Exactly(2));
        ActorUtils.VerifyGetMessage(actor, peer2, "Do", () => Times.Exactly(2));
    }

    [Fact]
    public void NotGrantedSinceNotResetTest()
    {
        var peer1 = MockActorFactory.Create("Peer1");
        var peer2 = MockActorFactory.Create("Peer2");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeer(peer1, peer2);

        actor.Start();
        var peer1ReadyToDoId = Guid.NewGuid();
        actor.Send(
            new Message(
                peer1,
                "_status",
                new Dict { ["MyActor"] = new Dict { ["ReadyToDo"] = peer1ReadyToDoId } }
            )
        );
        actor.Send(
            new Message(
                peer2,
                "_status",
                new Dict { ["MyActor"] = new Dict { ["ReadyToDo"] = Guid.NewGuid() } }
            )
        );
        actor.Send(
            new Message(
                peer1,
                "_status",
                new Dict
                {
                    ["MyActor"] = new Dict
                    {
                        ["ReadyToDo"] = peer1ReadyToDoId,
                        ["ReadyToDo2"] = Guid.NewGuid(),
                    },
                }
            )
        );

        actor.Send(new TerminateMessage());
        actor.Join();

        ActorUtils.VerifyGetMessage(actor, peer1, "Do", Times.Once);
        ActorUtils.VerifyGetMessage(actor, peer2, "Do", Times.Once);
        ActorUtils.VerifyGetMessage(actor, peer1, "Do2", Times.Never);
    }

    [Fact]
    public void GrantedSinceResetTest()
    {
        var peer1 = MockActorFactory.Create("Peer1");
        var peer2 = MockActorFactory.Create("Peer2");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeer(peer1, peer2);

        actor.Start();
        var peer1ReadyToDoId = Guid.NewGuid();
        actor.Send(
            new Message(
                peer1,
                "_status",
                new Dict { ["MyActor"] = new Dict { ["ReadyToDo"] = peer1ReadyToDoId } }
            )
        );
        actor.Send(
            new Message(
                peer2,
                "_status",
                new Dict { ["MyActor"] = new Dict { ["ReadyToDo"] = Guid.NewGuid() } }
            )
        );
        actor.Send(
            new Message(
                peer1,
                "_status",
                new Dict
                {
                    ["MyActor"] = new Dict
                    {
                        ["ReadyToDo"] = Guid.Empty,
                        ["ReadyToDo2"] = Guid.NewGuid(),
                    },
                }
            )
        );

        actor.Send(new TerminateMessage());
        actor.Join();

        ActorUtils.VerifyGetMessage(actor, peer1, "Do", Times.Once);
        ActorUtils.VerifyGetMessage(actor, peer2, "Do", Times.Once);
        ActorUtils.VerifyGetMessage(actor, peer1, "Do2", Times.Once);
    }

    private class TestActor : Actor
    {
        private readonly Dict _grants = new();
        private IActor Peer1;
        private IActor Peer2;

        public TestActor(ActorConfig config)
            : base(config) { }

        public void SetPeer(IActor peer1, IActor peer2)
        {
            Peer1 = peer1;
            Peer2 = peer2;
            InitPeers([Peer1, Peer2]);
        }

        protected override bool ProcessMessage(Message message)
        {
            base.ProcessMessage(message);
            switch (message.Name)
            {
                case "_status":
                    if (
                        SyncUtils.SyncRequestsCheck(
                            this,
                            _grants,
                            [
                                new RequestSource(Peer1, "ReadyToDo"),
                                new RequestSource(Peer2, "ReadyToDo"),
                            ],
                            "MyGrant"
                        )
                    )
                    {
                        Peer1.Send(new Message(this, "Do"));
                        Peer2.Send(new Message(this, "Do"));
                    }

                    if (
                        SyncUtils.SyncRequestsCheck(
                            this,
                            _grants,
                            [new RequestSource(Peer1, "ReadyToDo2")],
                            "MyGrant2"
                        )
                    )
                    {
                        Peer1.Send(new Message(this, "Do2"));
                    }

                    return true;
            }

            return false;
        }
    }
}
