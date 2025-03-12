using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(AutoState<>))]
public class AutoStateTest : ActorFactoryBase
{
    [Fact]
    public void AutoTest()
    {
        var sendMock = new SendMock();
        var syncer = MockActorFactory.Create("Syncer");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeers(syncer);
        actor.State = new IdleState(actor);
        actor.StateChanged += (sender, tuple) =>
        {
            var (oldState, newState) = tuple;
            switch (newState)
            {
                case AutoState:
                    ActorUtils.SendSignal(syncer, actor, "_auto", false);
                    break;
                case IdleState:
                    actor.Send(new TerminateMessage());
                    break;
            }
        };

        actor.Start();
        ActorUtils.SendSignal(syncer, actor, "_auto");
        actor.Join();
    }

    private class TestActor : Actor
    {
        public IActor Syncer = EmptyActor.Instance;

        public TestActor(ActorConfig config)
            : base(config) { }

        public override IState CreateIdleState()
        {
            return new IdleState(this);
        }

        public void SetPeers(IActor syncer)
        {
            Syncer = syncer;
            InitPeers([syncer]);
        }
    }

    private class IdleState(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "_status":
                    if (Actor.GetPeerStatus(actor.Syncer, "_auto") is true)
                    {
                        Actor.State = new AutoState(Actor, actor.Syncer);
                        return true;
                    }

                    break;
            }

            return false;
        }
    }

    private class AutoState(TestActor actor, IActor parent)
        : AutoState<TestActor>(actor, parent) { }
}
