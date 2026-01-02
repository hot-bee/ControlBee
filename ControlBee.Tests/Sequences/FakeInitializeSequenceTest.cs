using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;
using Xunit;

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(FakeInitializeSequence))]
public class FakeInitializeSequenceTest : ActorFactoryBase
{
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
    public void RunTest()
    {
        RecreateWithSkipWaitSensor();
        SetupWithDevice();
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Go"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();
        Assert.Equal(100.0, actor.X.GetPosition());
    }

    private class TestActor : Actor
    {
        public readonly IInitializeSequence InitializeSequenceX;
        public readonly IAxis X;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            X.GetInitPos()[0] = 100.0;
        }

        protected override void MessageHandler(Message message)
        {
            base.MessageHandler(message);
            if (message.Name == "Go")
                X.Initialize();
        }
    }
}
