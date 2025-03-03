using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(
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
    ISystemPropertiesDataSource systemPropertiesDataSource,
    IActorRegistry actorRegistry
) : IActorFactory
{
    public T Create<T>(string actorName, params object?[]? args)
        where T : IActorInternal
    {
        if (!typeof(IActor).IsAssignableFrom(typeof(T)))
            throw new ApplicationException(
                "Cannot create this object. It must be derived from the 'Actor' class."
            );
        var uiActor = actorRegistry.Get("ui");
        var actorConfig = new ActorConfig(
            actorName,
            systemConfigurations,
            axisFactory,
            digitalInputFactory,
            digitalOutputFactory,
            analogInputFactory,
            analogOutputFactory,
            dialogFactory,
            initializeSequenceFactory,
            binaryActuatorFactory,
            variableManager,
            timeManager,
            scenarioFlowTester,
            systemPropertiesDataSource,
            uiActor
        );
        var actorArgs = new List<object?> { actorConfig };
        if (args != null)
            actorArgs.AddRange(args);

        var actor = (T)Activator.CreateInstance(typeof(T), actorArgs.ToArray())!;
        actor.Init(actorConfig);
        actorRegistry?.Add(actor);
        return actor;
    }
}
