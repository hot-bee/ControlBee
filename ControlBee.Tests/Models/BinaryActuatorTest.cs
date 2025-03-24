using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.Utils;
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
    public void TimeoutBothTest()
    {
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
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");
        ScenarioFlowTester.Setup(
            [
                [
                    // ReSharper disable once AccessToDisposedClosure
                    new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.CylFwdDet1).On = true),
                ],
            ]
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "On1"));
        actor.Send(new Message(EmptyActor.Instance, "_terminate"));
        actor.Join();

        Assert.True(actor.Cyl1.IsOn());
        var match = new Func<Message, bool>(message => message.Name == "_displayDialog");
        Mock.Get(ui).Verify(m => m.Send(It.Is<Message>(message => match(message))), Times.Never);
        Assert.True(TimeManager.CurrentMilliseconds is > 1000 and < 2000);
    }

    [Fact]
    public void OnAndWaitBothTest()
    {
        var ui = Mock.Of<IUiActor>();
        Mock.Get(ui).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(ui);
        var actor = ActorFactory.Create<TestActor>("myActor");
        ScenarioFlowTester.Setup(
            [
                [
                    // ReSharper disable once AccessToDisposedClosure
                    new ConditionStep(() => TimeManager.CurrentMilliseconds > 1000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.CylFwdDet1).On = true),
                    new ConditionStep(() => TimeManager.CurrentMilliseconds > 2000),
                    new BehaviorStep(() => ((FakeDigitalInput)actor.CylFwdDet2).On = true),
                ],
            ]
        );

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
            SystemConfigurations = new SystemConfigurations
            {
                FakeMode = true,
                SkipWaitSensor = true,
            },
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
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && !(bool)actorItemMessage.DictPayload!["On"]!
                && actorItemMessage.DictPayload!["IsOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && !(bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.Once);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && actorItemMessage.DictPayload!["IsOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && (bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match2(message))), Times.AtLeastOnce);

        var match3 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && actorItemMessage.DictPayload!["IsOn"] is true
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
            SystemConfigurations = new SystemConfigurations
            {
                FakeMode = true,
                SkipWaitSensor = true,
            },
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
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && actorItemMessage.DictPayload!["IsOn"] == null
                && !(bool)actorItemMessage.DictPayload!["OffDetect"]!
                && (bool)actorItemMessage.DictPayload!["OnDetect"]!;
        });
        Mock.Get(uiActor)
            .Verify(m => m.Send(It.Is<Message>(message => match1(message))), Times.AtLeastOnce);

        var match2 = new Func<Message, bool>(message =>
        {
            var actorItemMessage = (ActorItemMessage)message;
            return actorItemMessage
                    is { Name: "_itemDataChanged", ActorName: "MyActor", ItemPath: "/Cyl1" }
                && (bool)actorItemMessage.DictPayload!["On"]!
                && actorItemMessage.DictPayload!["IsOn"] is true
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
            SystemConfigurations = new SystemConfigurations
            {
                FakeMode = true,
                SkipWaitSensor = true,
            },
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

        Assert.False(actor.Cyl1.GetCommandOn());
        Assert.True(actor.Cyl1.IsOff());
        Assert.True(actor.Cyl1.OffDetect());
        Assert.False(actor.Cyl1.OnDetect());
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

            return false;
        }
    }
}
