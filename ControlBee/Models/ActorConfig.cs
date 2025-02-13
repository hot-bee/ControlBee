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
    IVariableManager variableManager,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    IActorItemInjectionDataSource actorItemInjectionDataSource,
    IActor? uiActor
)
{
    public SystemConfigurations SystemConfigurations { get; } = systemConfigurations;
    public IBinaryActuatorFactory BinaryActuatorFactory { get; } = binaryActuatorFactory;
    public IScenarioFlowTester ScenarioFlowTester { get; } = scenarioFlowTester;
    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalInputFactory DigitalInputFactory => digitalInputFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
    public IAnalogInputFactory AnalogInputFactory { get; } = analogInputFactory;
    public IAnalogOutputFactory AnalogOutputFactory { get; } = analogOutputFactory;
    public IDialogFactory DialogFactory { get; } = dialogFactory;
    public IInitializeSequenceFactory InitializeSequenceFactory => initializeSequenceFactory;

    public IActorItemInjectionDataSource ActorItemInjectionDataSource =>
        actorItemInjectionDataSource;

    public IActor? UiActor => uiActor;
}
