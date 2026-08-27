using System;
using System.Collections.Generic;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(BinaryActuator))]
public class BinaryActuatorTest
    //: ActorFactoryBase(new SystemConfigurations { FakeMode = true, SkipWaitSensor = true })
    : ActorFactoryBase
{
    [Fact]
    public void TimeoutTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
            message.Name == "_displayDialog"
            && ((IDialogContext)message.Payload!).ItemPath == "/Cyl1/OnTimeoutError"
        );
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(TimeManager.CurrentMilliseconds >= 5000);
    }

    [Fact]
    public void OffTimeoutTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        SystemPropertiesDataSource.ReadFromString(
            """
              myActor:
                CylFwdDet1:
                  DeviceName: MyDevice
                  Channel: 0
                CylBwdDet1:
                  DeviceName: MyDevice
                  Channel: 2
            """
        );
        var device = Mock.Of<IDigitalIoDevice>();
        DeviceManager.Add("MyDevice", device);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(true);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(2)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "OnOff1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
            message.Name == "_displayDialog"
            && ((IDialogContext)message.Payload!).ItemPath == "/Cyl1/OffTimeoutError"
        );
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
    }

    [Fact]
    public void TimeoutBothTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "OnBoth"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(TimeManager.CurrentMilliseconds is >= 5000 and < 6000);
    }

    [Fact]
    public void OnAndWaitTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");
        ScenarioFlowTester.Setup([
            [
                // ReSharper disable once AccessToDisposedClosure
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(true)
                ),
            ],
        ]);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl1.IsOn());
        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
        Assert.True(TimeManager.CurrentMilliseconds is > 1000 and < 2000);
    }

    private IDigitalIoDevice SetupDevice()
    {
        SystemPropertiesDataSource.ReadFromString(
            """
              myActor:
                CylFwdDet1:
                  DeviceName: MyDevice
                  Channel: 0
                CylFwdDet2:
                  DeviceName: MyDevice
                  Channel: 1
            """
        );

        var device = Mock.Of<IDigitalIoDevice>();
        DeviceManager.Add("MyDevice", device);
        return device;
    }

    [Fact]
    public void OnAndWaitBothTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(1)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");
        ScenarioFlowTester.Setup([
            [
                // ReSharper disable once AccessToDisposedClosure
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(true)
                ),
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 2000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(1)).Returns(true)
                ),
            ],
        ]);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "OnBoth"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl1.IsOn());
        Assert.True(actor.Cyl2.IsOn());
        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
        Assert.True(TimeManager.CurrentMilliseconds is > 2000 and < 3000);
    }

    [Fact]
    public void OnAndTimeoutTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(TimeManager.CurrentMilliseconds >= 5000);
    }

    [Fact]
    public void DataChangedTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = true },
        };
        Recreate(config);

        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new ActorItemMessage(uiActor, "/Cyl1", "_itemDataRead"));
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && !(bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && !(bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && !(bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.AtLeastOnce);

        var match3 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] is true
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && (bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match3(message))), Times.Once);
    }

    [Fact]
    public void DataWriteTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = true },
        };
        Recreate(config);

        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Cyl1",
                "_itemDataWrite",
                new Dictionary<string, object?> { ["On"] = true }
            )
        );
        actor.Send(new Message(EmptyActor.Instance, "Wait"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match1 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && !(bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.AtLeastOnce);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] is true
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && (bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.AtLeastOnce);
    }

    [Fact]
    public void OnAndOffTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = true },
        };
        Recreate(config);

        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "OnAndOff"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.False(actor.Cyl1.IsOn(CommandActualType.Command));
        Assert.True(actor.Cyl1.IsOff());
        Assert.True(actor.Cyl1.OffDetect());
        Assert.False(actor.Cyl1.OnDetect());
    }

    [Fact]
    public void OffOutputDataWriteTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = true },
        };
        Recreate(config);

        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/CylBwd1",
                "_itemDataWrite",
                new Dictionary<string, object?> { ["On"] = true }
            )
        );
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message =>
        {
            var actorItemMessage = message as ActorItemMessage;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && !(bool)actorItemMessage.DictPayload!["CommandOn"]!
                && actorItemMessage.DictPayload!["ActualOn"] == null
                && (bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.AtLeastOnce);

        Assert.True(actor.Cyl1.IsOn());
        Assert.True(actor.CylFwd1.IsOn(CommandActualType.Command));
        Assert.False(actor.CylBwd1.IsOn(CommandActualType.Command));
    }

    private class TestActor : Actor
    {
        public readonly IBinaryActuator Cyl1;

        public readonly IBinaryActuator Cyl2;
        public readonly IDigitalOutput CylBwd1 = new DigitalOutputPlaceholder();
        public readonly IDigitalOutput CylBwd2 = new DigitalOutputPlaceholder();
        public readonly IDigitalInput CylBwdDet1 = new DigitalInputPlaceholder();
        public readonly IDigitalInput CylBwdDet2 = new DigitalInputPlaceholder();
        public readonly IDigitalOutput CylFwd1 = new DigitalOutputPlaceholder();
        public readonly IDigitalOutput CylFwd2 = new DigitalOutputPlaceholder();
        public readonly IDigitalInput CylFwdDet1 = new DigitalInputPlaceholder();
        public readonly IDigitalInput CylFwdDet2 = new DigitalInputPlaceholder();

        public TestActor(ActorConfig config)
            : base(config)
        {
            Cyl1 = config.BinaryActuatorFactory.Create(CylFwd1, CylBwd1, CylFwdDet1, CylBwdDet1);
            Cyl2 = config.BinaryActuatorFactory.Create(CylFwd2, CylBwd2, CylFwdDet2, CylBwdDet2);
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<TestActor>(this, error);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "On1":
                    try
                    {
                        Cyl1.OnAndWait();
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }
                    return true;
                case "OnAndOff":
                    Cyl1.OnAndWait();
                    Cyl1.OffAndWait();
                    return true;

                case "OnOff1":
                    try
                    {
                        Cyl1.OnAndWait();
                        Cyl1.OffAndWait();
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }
                    return true;

                case "OnBoth":
                    try
                    {
                        Cyl1.On();
                        Cyl2.On();
                        Cyl1.Wait();
                        Cyl2.Wait();
                    }
                    catch (TimeoutError)
                    {
                        // Empty
                    }

                    return true;
                case "Wait":
                    Cyl1.Wait();
                    Cyl2.Wait();
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }

    [Fact]
    public void OnAndWaitMultipleInputsTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupMultipleInputDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(1)).Returns(false);
        var actor = ActorFactory.Create<MultipleInputTestActor>("myActor");
        ScenarioFlowTester.Setup([
            [
                // ReSharper disable once AccessToDisposedClosure
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(true)
                ),
                new BehaviorStep(() => Assert.Null(actor.Cyl.IsOn())),
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 2000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(1)).Returns(true)
                ),
            ],
        ]);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl.IsOn());
        Assert.True(ScenarioFlowTester.Complete);
        Assert.True(TimeManager.CurrentMilliseconds is > 2000 and < 3000);
    }

    [Fact]
    public void OnAndWaitMultipleInputsTimeoutTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupMultipleInputDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(false);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(1)).Returns(false);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<MultipleInputTestActor>("myActor");
        ScenarioFlowTester.Setup([
            [
                // ReSharper disable once AccessToDisposedClosure
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(0)).Returns(true)
                ),
            ],
        ]);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Once);
        Assert.True(ScenarioFlowTester.Complete);
        Assert.True(TimeManager.CurrentMilliseconds >= 5000);
    }

    [Fact]
    public void OffAndWaitMultipleInputsTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var device = SetupMultipleInputDevice();
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(2)).Returns(false);
        Mock.Get(device).Setup(m => m.GetDigitalInputBit(3)).Returns(false);
        var actor = ActorFactory.Create<MultipleInputTestActor>("myActor");
        ScenarioFlowTester.Setup([
            [
                // ReSharper disable once AccessToDisposedClosure
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(2)).Returns(true)
                ),
                new BehaviorStep(() => Assert.Null(actor.Cyl.IsOff())),
                new ConditionStep(() => TimeManager.CurrentMilliseconds > 2000),
                new BehaviorStep(() =>
                    Mock.Get(device).Setup(m => m.GetDigitalInputBit(3)).Returns(true)
                ),
            ],
        ]);

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "Off"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl.IsOff());
        Assert.True(ScenarioFlowTester.Complete);
        Assert.True(TimeManager.CurrentMilliseconds is > 2000 and < 3000);
    }

    [Fact]
    public void MultipleFakeDigitalInputsTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = true },
        };
        Recreate(config);

        var actor = ActorFactory.Create<MultipleInputTestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        var inputOn1 = Assert.IsType<FakeDigitalInput>(actor.CylFwdDet1);
        var inputOn2 = Assert.IsType<FakeDigitalInput>(actor.CylFwdDet2);
        var inputOff1 = Assert.IsType<FakeDigitalInput>(actor.CylBwdDet1);
        var inputOff2 = Assert.IsType<FakeDigitalInput>(actor.CylBwdDet2);
        Assert.True(inputOn1.IsOn());
        Assert.True(inputOn2.IsOn());
        Assert.True(inputOff1.IsOff());
        Assert.True(inputOff2.IsOff());
    }

    private IDigitalIoDevice SetupMultipleInputDevice()
    {
        SystemPropertiesDataSource.ReadFromString(
            """
              myActor:
                CylFwdDet1:
                  DeviceName: MyDevice
                  Channel: 0
                CylFwdDet2:
                  DeviceName: MyDevice
                  Channel: 1
                CylBwdDet1:
                  DeviceName: MyDevice
                  Channel: 2
                CylBwdDet2:
                  DeviceName: MyDevice
                  Channel: 3
            """
        );

        var device = Mock.Of<IDigitalIoDevice>();
        DeviceManager.Add("MyDevice", device);
        return device;
    }

    private class MultipleInputTestActor : Actor
    {
        public readonly IBinaryActuator Cyl;
        public readonly IDigitalOutput CylBwd = new DigitalOutputPlaceholder();
        public readonly IDigitalInput CylBwdDet1 = new DigitalInputPlaceholder();
        public readonly IDigitalInput CylBwdDet2 = new DigitalInputPlaceholder();
        public readonly IDigitalOutput CylFwd = new DigitalOutputPlaceholder();
        public readonly IDigitalInput CylFwdDet1 = new DigitalInputPlaceholder();
        public readonly IDigitalInput CylFwdDet2 = new DigitalInputPlaceholder();

        public MultipleInputTestActor(ActorConfig config)
            : base(config)
        {
            Cyl = config.BinaryActuatorFactory.Create(
                CylFwd,
                CylBwd,
                [CylFwdDet1, CylFwdDet2],
                [CylBwdDet1, CylBwdDet2]
            );
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<MultipleInputTestActor>(this, error);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "On":
                    try
                    {
                        Cyl.OnAndWait();
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }
                    return true;
                case "Off":
                    try
                    {
                        Cyl.OffAndWait();
                    }
                    catch (TimeoutError)
                    {
                        // Alert trigger will be checked.
                    }
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }

    [Fact]
    public void MultipleInputsNullTest()
    {
        var config = new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations { FakeMode = false },
        };
        Recreate(config);

        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<NullMultipleInputTestActor>("myActor");

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl.IsOn());
        Assert.False(actor.Cyl.OnDetect());
        Assert.False(actor.Cyl.OffDetect());
        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
        Assert.True(TimeManager.CurrentMilliseconds < 1000);
    }

    private class NullMultipleInputTestActor : Actor
    {
        public readonly IBinaryActuator Cyl;
        public readonly IDigitalOutput CylBwd = new DigitalOutputPlaceholder();
        public readonly IDigitalOutput CylFwd = new DigitalOutputPlaceholder();

        public NullMultipleInputTestActor(ActorConfig config)
            : base(config)
        {
            Cyl = config.BinaryActuatorFactory.Create(
                CylFwd,
                CylBwd,
                (IDigitalInput[]?)null,
                (IDigitalInput[]?)null
            );
        }

        protected override IState CreateErrorState(SequenceError error)
        {
            return new ErrorState<NullMultipleInputTestActor>(this, error);
        }

        protected override bool ProcessMessage(Message message)
        {
            if (message.Name == "On")
            {
                Cyl.OnAndWait();
                return true;
            }

            return base.ProcessMessage(message);
        }
    }
}
