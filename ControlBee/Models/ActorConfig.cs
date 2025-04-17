using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    SystemConfigurations systemConfigurations,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IAnalogInputFactory analogInputFactory,
    IAnalogOutputFactory analogOutputFactory,
    IDialogFactory dialogFactory,
    IInitializeSequenceFactory initializeSequenceFactory,
    IBinaryActuatorFactory binaryActuatorFactory,
    IVisionFactory visionFactory,
    IVariableManager variableManager,
    IEventWriter eventWriter,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    ISystemPropertiesDataSource systemPropertiesDataSource,
    IDeviceManager deviceManager,
    IActor? uiActor
)
{
    public SystemConfigurations SystemConfigurations { get; } = systemConfigurations;
    public IBinaryActuatorFactory BinaryActuatorFactory { get; } = binaryActuatorFactory;
    public IVisionFactory VisionFactory { get; } = visionFactory;
    public IScenarioFlowTester ScenarioFlowTester { get; } = scenarioFlowTester;
    public IDeviceManager DeviceManager { get; } = deviceManager;
    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public IEventWriter EventWriter => eventWriter;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalInputFactory DigitalInputFactory => digitalInputFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
    public IAnalogInputFactory AnalogInputFactory { get; } = analogInputFactory;
    public IAnalogOutputFactory AnalogOutputFactory { get; } = analogOutputFactory;
    public IDialogFactory DialogFactory { get; } = dialogFactory;

    public IInitializeSequenceFactory InitializeSequenceFactory => initializeSequenceFactory;

    public ISystemPropertiesDataSource SystemPropertiesDataSource => systemPropertiesDataSource;

    public IActor? UiActor => uiActor;
}
