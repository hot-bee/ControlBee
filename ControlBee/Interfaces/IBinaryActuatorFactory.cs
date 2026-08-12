namespace ControlBee.Interfaces;

public interface IBinaryActuatorFactory
{
    IBinaryActuator Create(
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    );

    IBinaryActuator Create(
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput[]? inputsOn,
        IDigitalInput[]? inputsOff
    );
}
