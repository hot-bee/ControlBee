using ControlBee.Services;
using ControlBee.Variables;

namespace ControlBee.Models;

public class EmptyActor
{
    public static Actor Instance = new(
        new ActorConfig(
            "_empty",
            new SystemConfigurations(),
            EmptyAxisFactory.Instance,
            EmptyDigitalInputFactory.Instance,
            EmptyDigitalOutputFactory.Instance,
            null!,
            null!,
            EmptyInitializeSequenceFactory.Instance,
            EmptyBinaryActuatorFactory.Instance,
            EmptyVariableManager.Instance,
            EmptyTimeManager.Instance,
            EmptyScenarioFlowTester.Instance,
            EmptyActorItemInjectionDataSource.Instance,
            null
        )
    );
}
