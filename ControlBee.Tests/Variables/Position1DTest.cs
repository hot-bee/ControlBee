using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Position1D))]
public class Position1DTest : ActorFactoryBase
{
    [Fact]
    public void InitialValuesTest()
    {
        var position = new Position1D(DenseVector.OfArray([1]));
        Assert.Equal(DenseVector.OfArray([1]), position.Vector);
    }

    [Fact]
    public void ItemDataReadTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (Position1D)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual([]));
                Assert.Equal(1.0, newValue[0]);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(new ActorItemMessage(uiActor, "/MyVariable", "_itemDataRead"));

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void ItemDataWriteTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (double)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual([0]));
                Assert.Equal(3.0, newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/MyVariable",
                "_itemDataWrite",
                new ItemDataWriteArgs([0], 3.0)
            )
        );

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void ItemDataChangedTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (double)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual([0]));
                Assert.Equal(2.0, newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(new Message(actor, "ChangeVariable"));

        actor.Start();
        actor.Join();
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position1D> MyVariable = new(
            VariableScope.Temporary,
            new Position1D(DenseVector.OfArray([1.0]))
        );

        public TestActor(ActorConfig config)
            : base(config) { }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "ChangeVariable":
                    MyVariable.Value[0] = 2.0;
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }
}
