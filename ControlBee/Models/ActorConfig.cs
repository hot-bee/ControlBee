using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    ISystemConfigurations systemConfigurations,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IAnalogInputFactory analogInputFactory,
    IAnalogOutputFactory analogOutputFactory,
    IDialogFactory dialogFactory,
    IInitializeSequenceFactory initializeSequenceFactory,
    IBinaryActuatorFactory binaryActuatorFactory,
    IVisionFactory visionFactory,
    ICounterFactory counterFactory,
    IVariableManager variableManager,
    IEventManager eventManager,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    ISystemPropertiesDataSource systemPropertiesDataSource,
    IDeviceManager deviceManager,
    ILocalizationManager localizationManager,
    IActor? uiActor
)
{
    public ISystemConfigurations SystemConfigurations { get; } = systemConfigurations;
    public IBinaryActuatorFactory BinaryActuatorFactory { get; } = binaryActuatorFactory;
    public IVisionFactory VisionFactory { get; } = visionFactory;
    public ICounterFactory CounterFactory { get; } = counterFactory;
    public IScenarioFlowTester ScenarioFlowTester { get; } = scenarioFlowTester;
    public IDeviceManager DeviceManager { get; } = deviceManager;
    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public IEventManager EventManager => eventManager;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalInputFactory DigitalInputFactory => digitalInputFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
    public IAnalogInputFactory AnalogInputFactory { get; } = analogInputFactory;
    public IAnalogOutputFactory AnalogOutputFactory { get; } = analogOutputFactory;
    public IDialogFactory DialogFactory { get; } = dialogFactory;

    public IInitializeSequenceFactory InitializeSequenceFactory => initializeSequenceFactory;

    public ISystemPropertiesDataSource SystemPropertiesDataSource => systemPropertiesDataSource;

    public ILocalizationManager LocalizationManager => localizationManager;

    public IActor? UiActor => uiActor;
}
