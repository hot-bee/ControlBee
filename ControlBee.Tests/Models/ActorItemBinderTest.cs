using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;
using Message = ControlBee.Models.Message;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ActorItemBinder))]
public class ActorItemBinderTest : ActorFactoryBase
{
    [Fact]
    public void DataChangedTest()
    {
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");

        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<Actor>("MyActor");
        var binder = new ActorItemBinder(ActorRegistry, "MyActor", "/MyVar");
        var called = false;
        binder.DataChanged += (sender, args) =>
        {
            var valueChangedArgs = args[nameof(ValueChangedArgs)] as ValueChangedArgs;
            Assert.Equal([], valueChangedArgs?.Location);
            Assert.Null(valueChangedArgs?.OldValue);
            Assert.Equal(1, valueChangedArgs?.NewValue);
            called = true;
        };
        Mock.Get(uiActor)
            .Raise(
                m => m.MessageArrived += null,
                uiActor,
                new ActorItemMessage(
                    actor,
                    "/MyVar",
                    "_itemDataChanged",
                    new Dict { [nameof(ValueChangedArgs)] = new ValueChangedArgs([], null, 1) }
                )
            );
        Assert.True(called);
    }

    [Fact]
    public void BindingTest()
    {
        var uiActor = ActorFactory.Create<UiActor>("Ui");
        var actor = ActorFactory.Create<Actor>("MyActor");
        uiActor.SetHandler(new DirectUiActorMessageHandler());
        var variable = new Variable<int>(VariableScope.Global, 1);
        actor.AddItem(variable, "/MyVar");

        var actorItemInjectionDataSource = Mock.Of<ISystemPropertiesDataSource>();
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Name"))
            .Returns("My variable");
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Unit"))
            .Returns("bool");
        Mock.Get(actorItemInjectionDataSource)
            .Setup(m => m.GetValue("MyActor", "/MyVar", "Desc"))
            .Returns("This is a my variable.");
        variable.InjectProperties(actorItemInjectionDataSource);

        var binder = new ActorItemBinder(ActorRegistry, "MyActor", "/MyVar");
        var metaDataChangedCall = false;
        binder.MetaDataChanged += (sender, metaData) =>
        {
            Assert.Equal("My variable", metaData["Name"]);
            Assert.Equal("bool", metaData["Unit"]);
            Assert.Equal("This is a my variable.", metaData["Desc"]);
            metaDataChangedCall = true;
        };
        var callCount = 0;
        binder.DataChanged += (sender, args) =>
        {
            var valueChangedArgs = args[nameof(ValueChangedArgs)] as ValueChangedArgs;
            switch (callCount)
            {
                case 0:
                    callCount++;
                    Assert.Equal([], valueChangedArgs?.Location);
                    Assert.Null(valueChangedArgs?.OldValue);
                    Assert.Equal(1, valueChangedArgs?.NewValue);
                    variable.Value = 2;
                    break;
                case 1:
                    callCount++;
                    Assert.Equal([], valueChangedArgs?.Location);
                    Assert.Equal(1, valueChangedArgs?.OldValue);
                    Assert.Equal(2, valueChangedArgs?.NewValue);
                    actor.Send(new Message(EmptyActor.Instance, "_terminate"));
                    break;
            }
        };

        actor.Start();
        actor.Join();
        Assert.Equal(2, callCount);
        Assert.True(metaDataChangedCall);
    }

    [Fact]
    public void GetAxisItemPathsTest()
    {
        var actor = ActorFactory.Create<TestActor>("MyActor");
        var axisItemPaths = actor.GetAxisItemPaths("/PositionXY");
        Assert.Equal(["/X", "/Y"], axisItemPaths);
    }

    private class TestActor : Actor
    {
        public readonly Variable<Position2D> PositionXY = new();
        public readonly IAxis X;
        public readonly IAxis Y;

        public TestActor(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Y = config.AxisFactory.Create();
            PositionAxesMap.Add(PositionXY, [X, Y]);
        }
    }
}
