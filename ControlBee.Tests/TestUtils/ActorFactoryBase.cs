using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;
using Moq;

namespace ControlBee.Tests.TestUtils;

public abstract class ActorFactoryBase : IDisposable
{
    protected SystemConfigurations SystemConfigurations;
    protected IActorFactory ActorFactory;
    protected IActorRegistry ActorRegistry;
    protected ISystemPropertiesDataSource SystemPropertiesDataSource;
    protected IInitializeSequenceFactory InitializeSequenceFactory;
    protected IDigitalOutputFactory DigitalOutputFactory;
    protected IDigitalInputFactory DigitalInputFactory;
    protected IAnalogOutputFactory AnalogOutputFactory;
    protected IAnalogInputFactory AnalogInputFactory;
    protected IDialogFactory DialogFactory;
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
        // ReSharper disable once VirtualMemberCallInConstructor
        Setup();
    }

    protected ActorFactoryBase()
        : this(new ActorFactoryBaseConfig()) { }

    public virtual void Dispose()
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        TimeManager?.Dispose();
    }

    public void RecreateWithSkipWaitSensor()
    {
        Recreate(
            new ActorFactoryBaseConfig
            {
                SystemConfigurations = new SystemConfigurations
                {
                    FakeMode = true,
                    SkipWaitSensor = true,
                },
            }
        );
        Setup();
    }

    public virtual void Setup() { }

    public void Recreate(ActorFactoryBaseConfig config)
    {
        Dispose();
        SystemConfigurations =
            config.SystemConfigurations ?? new SystemConfigurations { FakeMode = true };
        ScenarioFlowTester = config.ScenarioFlowTester ?? new ScenarioFlowTester();
        TimeManager =
            config.TimeManager ?? new FrozenTimeManager(SystemConfigurations, ScenarioFlowTester);
        Database = config.Database ?? Mock.Of<IDatabase>();
        DeviceManager = config.DeviceManager ?? new DeviceManager();
        var deviceMonitor = Mock.Of<IDeviceMonitor>();
        AxisFactory =
            config.AxisFactory
            ?? new AxisFactory(
                SystemConfigurations,
                DeviceManager,
                TimeManager,
                ScenarioFlowTester,
                deviceMonitor
            );
        ActorRegistry = config.ActorRegistry ?? new ActorRegistry();
        VariableManager = config.VariableManager ?? new VariableManager(Database, ActorRegistry);
        DigitalInputFactory =
            config.DigitalInputFactory
            ?? new DigitalInputFactory(
                SystemConfigurations,
                DeviceManager,
                ScenarioFlowTester,
                deviceMonitor
            );
        DigitalOutputFactory =
            config.DigitalOutputFactory
            ?? new DigitalOutputFactory(
                SystemConfigurations,
                DeviceManager,
                TimeManager,
                deviceMonitor
            );
        AnalogInputFactory =
            config.AnalogInputFactory
            ?? new AnalogInputFactory(SystemConfigurations, DeviceManager, deviceMonitor);
        AnalogOutputFactory =
            config.AnalogOutputFactory
            ?? new AnalogOutputFactory(SystemConfigurations, DeviceManager, deviceMonitor);
        DialogFactory = config.DialogFactory ?? new DialogFactory(new DialogContextFactory(), null);
        InitializeSequenceFactory =
            config.InitializeSequenceFactory ?? new InitializeSequenceFactory(SystemConfigurations);
        BinaryActuatorFactory =
            config.BinaryActuatorFactory
            ?? new BinaryActuatorFactory(SystemConfigurations, TimeManager, ScenarioFlowTester);
        SystemPropertiesDataSource =
            config.SystemPropertiesDataSource ?? new SystemPropertiesDataSource();
        ActorFactory =
            config.ActorFactory
            ?? new ActorFactory(
                SystemConfigurations,
                AxisFactory,
                DigitalInputFactory,
                DigitalOutputFactory,
                AnalogInputFactory,
                AnalogOutputFactory,
                DialogFactory,
                InitializeSequenceFactory,
                BinaryActuatorFactory,
                VariableManager,
                TimeManager,
                ScenarioFlowTester,
                SystemPropertiesDataSource,
                ActorRegistry
            );
    }
}
