using System.Collections.Generic;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.TestUtils;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(GlobalInitializeSequence))]
public class GlobalInitializeSequenceTest : ActorFactoryBase
{
    [Fact]
    public void RunMessageTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        var mandrel1 = Mock.Of<IActor>();
        var mandrel2 = Mock.Of<IActor>();
        var notSetActor = Mock.Of<IActor>();

        var globalInitializationSequence = new GlobalInitializeSequence(
            syncerActor,
            sequence =>
            {
                sequence.InitializeIfPossible(mandrel0);
                sequence.InitializeIfPossible(mandrel1);
                sequence.InitializeIfPossible(mandrel2);
                sequence.InitializeIfPossible(notSetActor);
                if (sequence.IsInitializingActors)
                    return;

                sequence.InitializeIfPossible(turret);
            },
            [mandrel0, mandrel1, mandrel2, turret]
        );
        globalInitializationSequence.Run();
        Mock.Get(mandrel0)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(mandrel1)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(mandrel2)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_resetState")),
                Times.Never
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );

        var action = () => globalInitializationSequence.Run();
        Assert.Throws<PlatformException>(() => action());
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel1,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel2,
            InitializationStatus.Initialized
        );

        Mock.Get(mandrel0).Invocations.Clear();
        Mock.Get(mandrel1).Invocations.Clear();
        Mock.Get(mandrel2).Invocations.Clear();
        Mock.Get(turret).Invocations.Clear();
        globalInitializationSequence.Run();

        Mock.Get(mandrel0)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(mandrel1)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(mandrel2)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_resetState")),
                Times.Once
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
    }

    [Fact]
    public void IsCompleteTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        var mandrel1 = Mock.Of<IActor>();
        var mandrel2 = Mock.Of<IActor>();
        var globalInitializationSequence = new GlobalInitializeSequence(
            syncerActor,
            _ => { },
            [mandrel0]
        );
        globalInitializationSequence.SetInitializationState(mandrel1, InitializationStatus.Skipped);
        globalInitializationSequence.SetInitializationState(mandrel2, InitializationStatus.Error);
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Initialized
        );
        Assert.IsFalse(globalInitializationSequence.IsComplete);
        Assert.IsFalse(globalInitializationSequence.IsInitializingActors);
        Assert.IsTrue(globalInitializationSequence.IsError);

        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initializing
        );
        Assert.IsFalse(globalInitializationSequence.IsComplete);
        Assert.IsTrue(globalInitializationSequence.IsInitializingActors);
        Assert.IsTrue(globalInitializationSequence.IsError);

        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Initialized
        );
        Assert.IsTrue(globalInitializationSequence.IsComplete);
        Assert.IsFalse(globalInitializationSequence.IsInitializingActors);
        Assert.IsTrue(globalInitializationSequence.IsError);
    }

    [Fact]
    public void StateChangedTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        Mock.Get(turret).Setup(m => m.Name).Returns("turret");
        Mock.Get(mandrel0).Setup(m => m.Name).Returns("mandrel0");
        var globalInitializationSequence = new GlobalInitializeSequence(syncerActor, _ => { }, []);

        var changedCalls = new List<(string actorName, InitializationStatus status)>();
        globalInitializationSequence.StateChanged += (sender, tuple) =>
        {
            changedCalls.Add(tuple);
        };
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initializing
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        Assert.AreEqual(4, changedCalls.Count);
        Assert.AreEqual("turret", changedCalls[0].actorName);
        Assert.AreEqual(InitializationStatus.Uninitialized, changedCalls[0].status);
        Assert.AreEqual("mandrel0", changedCalls[1].actorName);
        Assert.AreEqual(InitializationStatus.Uninitialized, changedCalls[1].status);
        Assert.AreEqual("mandrel0", changedCalls[2].actorName);
        Assert.AreEqual(InitializationStatus.Initializing, changedCalls[2].status);
        Assert.AreEqual("mandrel0", changedCalls[3].actorName);
        Assert.AreEqual(InitializationStatus.Initialized, changedCalls[3].status);
    }

    [Fact]
    public void InitializeTest()
    {
        var subActor = ActorFactory.Create<SubActor>("SubActor");
        var syncerActor = ActorFactory.Create<SyncerActor>("Syncer", subActor);

        subActor.Start();
        syncerActor.Start();

        var initializingActors = new IActor[] { subActor };
        syncerActor.Send(new Message(syncerActor, "_initialize", initializingActors));
        syncerActor.Send(new Message(syncerActor, "_terminate"));

        syncerActor.Join();
        subActor.Join();

        Assert.IsInstanceOfType<IdleState>(subActor.State);
    }

    [Fact]
    public void InitializeOnIdleTest()
    {
        var subActor = ActorFactory.Create<SubActor>("SubActor");
        var syncerActor = ActorFactory.Create<SyncerActor>("Syncer", subActor);

        subActor.State = new IdleState(subActor);

        var transited = false;
        subActor.MessageProcessed += (_, tuple) =>
        {
            var (_, oldState, newState, result) = tuple;
            if (
                oldState.GetType() == typeof(IdleState)
                && newState.GetType() == typeof(InactiveState)
            )
                transited = true;
        };

        syncerActor.Start();
        subActor.Start();

        var initializingActors = new IActor[] { subActor };
        syncerActor.Send(new Message(syncerActor, "_initialize", initializingActors));
        syncerActor.Send(new Message(syncerActor, "_terminate"));

        syncerActor.Join();
        subActor.Join();

        Assert.IsInstanceOfType<IdleState>(subActor.State);
        Assert.IsTrue(transited);
    }

    public class SubActor : Actor
    {
        public SubActor(ActorConfig config)
            : base(config)
        {
            State = new InactiveState(this);
        }
    }

    public class InactiveState(SubActor actor) : State<SubActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            if (message.Name == "_initialize")
            {
                Actor.Send(new Message(Actor, "_terminate"));
                Actor.State = new IdleState(Actor);
                return true;
            }

            return false;
        }
    }

    public class IdleState(SubActor actor) : State<SubActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            return false;
        }
    }

    public class SyncerActor(ActorConfig config, IActor subActor) : Actor(config)
    {
        protected override void MessageHandler(Message message)
        {
            base.MessageHandler(message);
            if (message.Name == "_initialize")
            {
                var initializingActors = (IEnumerable<IActor>)message.Payload!;
                var globalInitializationSequence = new GlobalInitializeSequence(
                    this,
                    seq =>
                    {
                        seq.InitializeIfPossible(subActor);
                    },
                    initializingActors
                );
                globalInitializationSequence.Run();
            }
        }
    }
}
