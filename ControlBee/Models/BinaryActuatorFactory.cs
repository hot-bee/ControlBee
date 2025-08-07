using ControlBee.Interfaces;

namespace ControlBee.Models;

public class BinaryActuatorFactory(
    ISystemConfigurations systemConfigurations,
    ITimeManager timeManager,
    IScenarioFlowTester scenarioFlowTester
) : IBinaryActuatorFactory
{
    public IBinaryActuator Create(
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    )
    {
        return new BinaryActuator(
            systemConfigurations,
            timeManager,
            scenarioFlowTester,
            outputOn,
            outputOff,
            inputOn,
            inputOff
        );
    }
}
