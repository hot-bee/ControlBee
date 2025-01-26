﻿using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(
    SystemConfigurations systemConfigurations,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IInitializeSequenceFactory initializeSequenceFactory,
    IVariableManager variableManager,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    IActorItemInjectionDataSource actorItemInjectionDataSource,
    IActorRegistry actorRegistry
)
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
            initializeSequenceFactory,
            variableManager,
            timeManager,
            scenarioFlowTester,
            actorItemInjectionDataSource,
            uiActor
        );
        var actorArgs = new List<object?> { actorConfig };
        if (args != null)
            actorArgs.AddRange(args);

        var actor = (T)Activator.CreateInstance(typeof(T), actorArgs.ToArray())!;
        actor.Init();
        actorRegistry?.Add(actor);
        return actor;
    }
}
