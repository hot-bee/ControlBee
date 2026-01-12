using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(State<>))]
public class StateTest : ActorFactoryBase
{
    [Fact]
    public void StateTransitionTest()
    {
        var actor = ActorFactory.Create<TestPickerActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Pickup"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Assert.AreEqual(1, actor.PickupCount.Value);
        Assert.IsTrue(actor.DisposeCount > 0);
    }

    public class TestPickerActor : Actor
    {
        public int DisposeCount;
        public Variable<int> PickupCount = new(VariableScope.Local);

        public TestPickerActor(ActorConfig config)
            : base(config)
        {
            State = new IdleState(this);
        }
    }

    public class IdleState(TestPickerActor actor) : State<TestPickerActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "Pickup":
                    Actor.State = new PickupState(Actor);
                    return true;
            }

            return false;
        }

        public override void Dispose()
        {
            Actor.DisposeCount++;
        }
    }

    public class PickupState(TestPickerActor actor) : State<TestPickerActor>(actor)
    {
        public override bool ProcessMessage(Message message)
        {
            Actor.PickupCount.Value++;
            Actor.State = new IdleState(Actor);
            return true;
        }
    }
}
