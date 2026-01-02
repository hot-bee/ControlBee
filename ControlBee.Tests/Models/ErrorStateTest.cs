using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ErrorState<>))]
public class ErrorStateTest : ActorFactoryBase
{
    [Fact]
    public void NormalTest()
    {
        var sendMock = new SendMock();
        var peer = MockActorFactory.Create("Peer");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.InitPeers([peer]);
        ActorUtils.SetupActionOnStateChanged(
            actor,
            typeof(IdleState),
            _ =>
            {
                Assert.True(actor.GetStatus("_error") is false);
                actor.Send(new TerminateMessage());
            }
        );
        sendMock.SetupActionOnSignal(
            actor,
            peer,
            "_error",
            message =>
            {
                actor.Send(new Message(EmptyActor.Instance, "_resetError"));
            }
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "DoSomethingWrong"));
        actor.Join();
    }

    private class TestActor : Actor
    {
        public TestActor(ActorConfig config)
            : base(config) { }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "DoSomethingWrong":
                    throw new SequenceError();
            }

            return base.ProcessMessage(message);
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<TestActor>(this, error);
        }

        public override IState CreateIdleState()
        {
            return new IdleState(this);
        }
    }

    private class IdleState(TestActor actor) : State<TestActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            return false;
        }
    }
}
