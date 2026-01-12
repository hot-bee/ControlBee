using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(ActorFactory))]
public class TestTest()
    : ActorFactoryBase(
        new ActorFactoryBaseConfig
        {
            SystemConfigurations = new SystemConfigurations
            {
                FakeMode = true,
                SkipWaitSensor = true,
            },
        }
    )
{
    [Fact]
    public void InitVariablesTest()
    {
        var variableManager = Mock.Of<IVariableManager>();
        Recreate(new ActorFactoryBaseConfig { VariableManager = variableManager });

        var actor = ActorFactory.Create<ActorWithVariables>("testActor");
        Mock.Get(variableManager).Verify(m => m.Add(actor.Foo), Times.Once);
        Mock.Get(variableManager).Verify(m => m.Add(actor.Bar), Times.Once);
        Mock.Get(variableManager).Verify(m => m.Add(actor.PickupPosition), Times.Once);

        Assert.AreEqual("testActor", actor.PickupPosition.ActorName);
        Assert.AreEqual("/PickupPosition", actor.PickupPosition.ItemPath);
        Assert.AreEqual("testActor", actor.PickupPosition.Value.Actor.Name);
        Assert.AreEqual("/PickupPosition", actor.PickupPosition.Value.ItemPath);
        Assert.AreEqual("testActor", actor.PickupPosition.Value[0, 0].Actor.Name);
        Assert.AreEqual("/PickupPosition", actor.PickupPosition.Value[0, 0].ItemPath);

        actor.PickupPosition.Value = new Array2D<Position1D>(10, 10);
        Assert.AreEqual("testActor", actor.PickupPosition.Value[0, 0].Actor.Name);
        Assert.AreEqual("/PickupPosition", actor.PickupPosition.Value[0, 0].ItemPath);

        Assert.AreEqual("testActor", actor.X.Actor.Name);
        Assert.AreEqual("/X", actor.X.ItemPath);
        Assert.IsInstanceOfType<FakeAxis>(actor.X);

        Assert.AreEqual("testActor", actor.Vacuum.Actor.Name);
        Assert.AreEqual("/Vacuum", actor.Vacuum.ItemPath);
        Assert.IsInstanceOfType<FakeDigitalOutput>(actor.Vacuum);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ActorWithVariables : Actor
    {
        public readonly Variable<double> Bar = new(VariableScope.Local);
        public readonly Variable<int> Foo = new(VariableScope.Global);

        public readonly Variable<Array2D<Position1D>> PickupPosition = new(
            VariableScope.Local,
            new Array2D<Position1D>(1, 1)
        );

        public readonly IDigitalOutput Vacuum;

        public readonly IAxis X;

        public ActorWithVariables(ActorConfig config)
            : base(config)
        {
            X = config.AxisFactory.Create();
            Vacuum = config.DigitalOutputFactory.Create();
        }
    }
}
