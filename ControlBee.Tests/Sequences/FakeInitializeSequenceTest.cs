using System.IO;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Tests.TestUtils;
using ControlBee.Variables;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(FakeInitializeSequence))]
public class FakeInitializeSequenceTest : ActorFactoryBase
{
    [Fact]
    public void RunTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Assert.Equal(100.0, actor.X.GetPosition());
    }

    public class TestActor : Actor
    {
        public Variable<Position1D> HomePositionX = new(
            VariableScope.Global,
            new Position1D(DenseVector.OfArray([100.0]))
        );
        public Variable<SpeedProfile> HomeSpeedX = new(
            VariableScope.Global,
            new SpeedProfile() { Velocity = 10.0 }
        );
        public IInitializeSequence InitializeSequenceX;
        public IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
            PositionAxesMap.Add(HomePositionX, [X]);
            InitializeSequenceX = InitializeSequenceFactory.Create(X, HomeSpeedX, HomePositionX);
        }

        protected override void ProcessMessage(Message message)
        {
            base.ProcessMessage(message);
            if (message.Name == "Go")
            {
                InitializeSequenceX.Run();
            }
        }
    }
}
