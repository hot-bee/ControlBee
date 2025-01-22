using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ActorItemBinder))]
public class ActorItemBinderTest
{
    [Fact]
    public void DataChangedTest()
    {
        var actorRegistry = new ActorRegistry();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("ui");
        var actor = new Actor("myActor");
        actorRegistry.Add(uiActor);
        actorRegistry.Add(actor);
        var binder = new ActorItemBinder(actorRegistry, "myActor", "/myVar");
        var called = false;
        binder.DataChanged += (sender, args) =>
        {
            Assert.Null(args.Location);
            Assert.Null(args.OldValue);
            Assert.Equal(1, args.NewValue);
            called = true;
        };
        Mock.Get(uiActor)
            .Raise(
                m => m.MessageArrived += null,
                uiActor,
                new ActorItemMessage(
                    actor,
                    "/myVar",
                    "_itemDataChanged",
                    new ValueChangedEventArgs(null, null, 1)
                )
            );
        Assert.True(called);
    }

    [Fact]
    public void BindingTest()
    {
        var database = Mock.Of<IDatabase>();
        var actorRegistry = new ActorRegistry();
        var variableManager = new VariableManager(database, actorRegistry);
        var uiActor = new UiActor(
            new ActorConfig(
                "ui",
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                variableManager,
                EmptyTimeManager.Instance
            )
        );
        var actor = new Actor("myActor");
        uiActor.SetHandler(new DirectUiActorMessageHandler());
        actorRegistry.Add(uiActor);
        actorRegistry.Add(actor);
        var variable = new Variable<int>(variableManager, actor, "/myVar", VariableScope.Global, 1);
        actor.AddItem(variable, "/myVar");

        var binder = new ActorItemBinder(actorRegistry, "myActor", "/myVar");
        var callCount = 0;
        binder.DataChanged += (sender, args) =>
        {
            switch (callCount)
            {
                case 0:
                    callCount++;
                    Assert.Null(args.Location);
                    Assert.Null(args.OldValue);
                    Assert.Equal(1, args.NewValue);
                    variable.Value = 2;
                    break;
                case 1:
                    callCount++;
                    Assert.Null(args.Location);
                    Assert.Equal(1, args.OldValue);
                    Assert.Equal(2, args.NewValue);
                    actor.Send(new Message(EmptyActor.Instance, "_terminate"));
                    break;
            }
        };

        actor.Start();
        actor.Join();
        Assert.Equal(2, callCount);
    }
}
