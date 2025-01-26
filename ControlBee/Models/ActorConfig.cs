using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    SystemConfigurations systemConfigurations,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IInitializeSequenceFactory initializeSequenceFactory,
    IVariableManager variableManager,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    IActorItemInjectionDataSource actorItemInjectionDataSource,
    IActor? uiActor
)
{
    public SystemConfigurations SystemConfigurations { get; } = systemConfigurations;
    public IScenarioFlowTester ScenarioFlowTester { get; } = scenarioFlowTester;
    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalInputFactory DigitalInputFactory => digitalInputFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
    public IInitializeSequenceFactory InitializeSequenceFactory => initializeSequenceFactory;

    public IActorItemInjectionDataSource ActorItemInjectionDataSource =>
        actorItemInjectionDataSource;

    public IActor? UiActor => uiActor;
}
