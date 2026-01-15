using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.TestUtils;
using ControlBee.Variables;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

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

        Assert.Equal("testActor", actor.PickupPosition.ActorName);
        Assert.Equal("/PickupPosition", actor.PickupPosition.ItemPath);
        Assert.Equal("testActor", actor.PickupPosition.Value.Actor.Name);
        Assert.Equal("/PickupPosition", actor.PickupPosition.Value.ItemPath);
        Assert.Equal("testActor", actor.PickupPosition.Value[0, 0].Actor.Name);
        Assert.Equal("/PickupPosition", actor.PickupPosition.Value[0, 0].ItemPath);

        actor.PickupPosition.Value = new Array2D<Position1D>(10, 10);
        Assert.Equal("testActor", actor.PickupPosition.Value[0, 0].Actor.Name);
        Assert.Equal("/PickupPosition", actor.PickupPosition.Value[0, 0].ItemPath);

        Assert.Equal("testActor", actor.X.Actor.Name);
        Assert.Equal("/X", actor.X.ItemPath);
        Assert.IsType<FakeAxis>(actor.X);

        Assert.Equal("testActor", actor.Vacuum.Actor.Name);
        Assert.Equal("/Vacuum", actor.Vacuum.ItemPath);
        Assert.IsType<FakeDigitalOutput>(actor.Vacuum);
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
