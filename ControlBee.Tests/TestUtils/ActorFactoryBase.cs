using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using Moq;

namespace ControlBee.Tests.TestUtils;

public abstract class ActorFactoryBase : IDisposable
{
    protected SystemConfigurations SystemConfigurations;
    protected IActorFactory MockActorFactory;
    protected IActorFactory ActorFactory;
    protected IActorRegistry ActorRegistry;
    protected IActorItemInjectionDataSource ActorItemInjectionDataSource;
    protected IInitializeSequenceFactory InitializeSequenceFactory;
    protected IDigitalOutputFactory DigitalOutputFactory;
    protected IDigitalInputFactory DigitalInputFactory;
    protected IBinaryActuatorFactory BinaryActuatorFactory;
    protected IVariableManager VariableManager;
    protected IAxisFactory AxisFactory;
    protected IScenarioFlowTester ScenarioFlowTester;
    protected ITimeManager TimeManager;
    protected IDeviceManager DeviceManager;
    protected IDatabase Database;

#pragma warning disable CS8618, CS9264
    protected ActorFactoryBase(ActorFactoryBaseConfig config)
#pragma warning restore CS8618, CS9264
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
            ?? new DigitalOutputFactory(SystemConfigurations, DeviceManager, TimeManager);
        InitializeSequenceFactory =
            config.InitializeSequenceFactory ?? new InitializeSequenceFactory(SystemConfigurations);
        BinaryActuatorFactory =
            config.BinaryActuatorFactory
            ?? new BinaryActuatorFactory(SystemConfigurations, TimeManager, ScenarioFlowTester);
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
                BinaryActuatorFactory,
                VariableManager,
                TimeManager,
                ScenarioFlowTester,
                ActorItemInjectionDataSource,
                ActorRegistry
            );
        MockActorFactory = new MockActorFactory(
            SystemConfigurations,
            AxisFactory,
            DigitalInputFactory,
            DigitalOutputFactory,
            InitializeSequenceFactory,
            BinaryActuatorFactory,
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
