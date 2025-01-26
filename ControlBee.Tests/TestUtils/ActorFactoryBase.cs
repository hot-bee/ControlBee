using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using Moq;

namespace ControlBee.Tests.TestUtils;

public abstract class ActorFactoryBase : IDisposable
{
    protected ActorFactory ActorFactory;
    protected ActorRegistry ActorRegistry;
    protected IActorItemInjectionDataSource ActorItemInjectionDataSource;
    protected InitializeSequenceFactory InitializeSequenceFactory;
    protected DigitalOutputFactory DigitalOutputFactory;
    protected DigitalInputFactory DigitalInputFactory;
    protected VariableManager VariableManager;
    protected AxisFactory AxisFactory;
    protected ScenarioFlowTester ScenarioFlowTester;
    protected FrozenTimeManager TimeManager;
    protected DeviceManager DeviceManager;
    protected SystemConfigurations SystemConfigurations;
    protected IDatabase Database;

    protected ActorFactoryBase(ActorFactoryBaseConfig config)
    {
        Recreate(config);
    }

    protected ActorFactoryBase()
        : this(new ActorFactoryBaseConfig()) { }

    public void Recreate(ActorFactoryBaseConfig config)
    {
        Dispose();
        TimeManager = config.TimeManager ?? new FrozenTimeManager();
        Database = config.Database ?? Mock.Of<IDatabase>();
        SystemConfigurations =
            config.SystemConfigurations ?? new SystemConfigurations() { FakeMode = true };
        DeviceManager = config.DeviceManager ?? new DeviceManager();
        ScenarioFlowTester = config.ScenarioFlowTester ?? new ScenarioFlowTester();
        AxisFactory =
            config.AxisFactory
            ?? new AxisFactory(
                SystemConfigurations,
                DeviceManager,
                TimeManager,
                ScenarioFlowTester
            );
        ActorRegistry = config.ActorRegistry ?? new ActorRegistry();
        VariableManager = config.VariableManager ?? new VariableManager(Database, ActorRegistry);
        DigitalInputFactory =
            config.DigitalInputFactory
            ?? new DigitalInputFactory(SystemConfigurations, DeviceManager, ScenarioFlowTester);
        DigitalOutputFactory =
            config.DigitalOutputFactory
            ?? new DigitalOutputFactory(SystemConfigurations, DeviceManager);
        InitializeSequenceFactory =
            config.InitializeSequenceFactory ?? new InitializeSequenceFactory(SystemConfigurations);
        ActorItemInjectionDataSource =
            config.ActorItemInjectionDataSource ?? new ActorItemInjectionDataSource();
        ActorFactory =
            config.ActorFactory
            ?? new ActorFactory(
                SystemConfigurations,
                AxisFactory,
                DigitalInputFactory,
                DigitalOutputFactory,
                InitializeSequenceFactory,
                VariableManager,
                TimeManager,
                ScenarioFlowTester,
                ActorItemInjectionDataSource,
                ActorRegistry
            );
    }

    public void Dispose()
    {
        TimeManager?.Dispose();
    }
}
