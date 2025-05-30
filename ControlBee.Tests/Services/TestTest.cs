﻿using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using FluentAssertions;
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
