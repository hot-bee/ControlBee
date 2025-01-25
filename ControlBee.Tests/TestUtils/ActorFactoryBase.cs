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

    protected ActorFactoryBase()
    {
        Database = Mock.Of<IDatabase>();
        SystemConfigurations = new SystemConfigurations { FakeMode = true };
        DeviceManager = new DeviceManager();
        TimeManager = new FrozenTimeManager();
        ScenarioFlowTester = new ScenarioFlowTester();
        AxisFactory = new AxisFactory(
            SystemConfigurations,
            DeviceManager,
            TimeManager,
            ScenarioFlowTester
        );
        VariableManager = new VariableManager(Database);
        DigitalInputFactory = new DigitalInputFactory(
            SystemConfigurations,
            DeviceManager,
            ScenarioFlowTester
        );
        DigitalOutputFactory = new DigitalOutputFactory(SystemConfigurations, DeviceManager);
        InitializeSequenceFactory = new InitializeSequenceFactory(SystemConfigurations);
        ActorItemInjectionDataSource = EmptyActorItemInjectionDataSource.Instance;
        ActorRegistry = new ActorRegistry();
        ActorFactory = new ActorFactory(
            AxisFactory,
            DigitalInputFactory,
            DigitalOutputFactory,
            InitializeSequenceFactory,
            VariableManager,
            TimeManager,
            ActorItemInjectionDataSource,
            ActorRegistry
        );
    }

    public void Dispose()
    {
        TimeManager.Dispose();
    }
}
