using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using Moq;

namespace ControlBee.Tests.TestUtils;

public class MockActorFactory(
    SystemConfigurations systemConfigurations,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IInitializeSequenceFactory initializeSequenceFactory,
    IBinaryActuatorFactory binaryActuatorFactory,
    IVariableManager variableManager,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester,
    IActorItemInjectionDataSource actorItemInjectionDataSource,
    IActorRegistry actorRegistry
) : IActorFactory
{
    public T Create<T>(string actorName, params object?[]? args)
        where T : class, IActorInternal
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
            binaryActuatorFactory,
            variableManager,
            timeManager,
            scenarioFlowTester,
            actorItemInjectionDataSource,
            uiActor
        );
        var actor = Mock.Of<T>();
        Mock.Get(actor).CallBase = true;
        actor.Init(actorConfig);
        actorRegistry?.Add(actor);
        return actor;
    }
}
