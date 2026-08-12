using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class EmptyBinaryActuatorFactory : IBinaryActuatorFactory
{
    public static EmptyBinaryActuatorFactory Instance = new();

    private EmptyBinaryActuatorFactory() { }

    public IBinaryActuator Create(
        IDigitalOutput outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    )
    {
        throw new UnimplementedByDesignError();
    }

    public IBinaryActuator Create(
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput[] inputsOn,
        IDigitalInput[] inputsOff
    )
    {
        throw new UnimplementedByDesignError();
    }
}
