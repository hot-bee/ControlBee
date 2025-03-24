using ControlBee.Services;
using ControlBee.Variables;

namespace ControlBee.Models;

public class EmptyActor : Actor
{
    public static Actor Instance = new EmptyActor(
        new ActorConfig(
            "_empty",
            new SystemConfigurations(),
            EmptyAxisFactory.Instance,
            EmptyDigitalInputFactory.Instance,
            EmptyDigitalOutputFactory.Instance,
            null!,
            null!,
            null!,
            EmptyInitializeSequenceFactory.Instance,
            EmptyBinaryActuatorFactory.Instance,
            null!,
            EmptyVariableManager.Instance,
            EmptyTimeManager.Instance,
            EmptyScenarioFlowTester.Instance,
            EmptySystemPropertiesDataSource.Instance,
            null
        )
    );

    private EmptyActor(ActorConfig config)
        : base(config) { }
}
