using System.IO;
using System.Threading;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(ActorFactory))]
public class ActorFactoryTest
{
    [Fact]
    public void InitVariablesTest()
    {
        var systemConfiguration = new SystemConfigurations
        {
            FakeMode = true,
            SkipWaitSensor = true,
        };
        var timeManager = Mock.Of<ITimeManager>();
        var deviceManager = Mock.Of<IDeviceManager>();
        var scenarioFlowTester = Mock.Of<IScenarioFlowTester>();
        var variableManager = Mock.Of<IVariableManager>();
        var axisFactory = new AxisFactory(
            systemConfiguration,
            deviceManager,
            timeManager,
            scenarioFlowTester
        );
        var digitalOutputFactory = new DigitalOutputFactory(systemConfiguration, deviceManager);
        var actorFactory = new ActorFactory(
            axisFactory,
            EmptyDigitalInputFactory.Instance,
            digitalOutputFactory,
            variableManager,
            timeManager,
            Mock.Of<IActorRegistry>()
        );
        var actor = actorFactory.Create<ActorWithVariables>("testActor");
        Mock.Get(variableManager).Verify(m => m.Add(actor.Foo), Times.Once);
        Mock.Get(variableManager).Verify(m => m.Add(actor.Bar), Times.Once);
        Mock.Get(variableManager).Verify(m => m.Add(actor.PickupPosition), Times.Once);

        actor.PickupPosition.ActorName.Should().Be("testActor");
        actor.PickupPosition.ItemPath.Should().Be("/PickupPosition");
        actor.PickupPosition.Value.Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value.ItemPath.Should().Be("/PickupPosition");
        actor.PickupPosition.Value[0, 0].Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value[0, 0].ItemPath.Should().Be("/PickupPosition");

        actor.PickupPosition.Value = new Array2D<Position1D>(10, 10);
        actor.PickupPosition.Value[0, 0].Actor.Name.Should().Be("testActor");
        actor.PickupPosition.Value[0, 0].ItemPath.Should().Be("/PickupPosition");

        Assert.Equal("testActor", actor.X.Actor.Name);
        Assert.Equal("/X", actor.X.ItemPath);
        Assert.Equal(typeof(FakeAxis), actor.X.GetType());

        Assert.Equal("testActor", actor.Vacuum.Actor.Name);
        Assert.Equal("/Vacuum", actor.Vacuum.ItemPath);
        Assert.Equal(typeof(FakeDigitalOutput), actor.Vacuum.GetType());
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

        public IDigitalOutput Vacuum;

        public IAxis X;

        public ActorWithVariables(ActorConfig config)
            : base(config)
        {
            X = AxisFactory.Create();
            Vacuum = DigitalOutputFactory.Create();
        }
    }
}
