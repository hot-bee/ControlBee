namespace ControlBee.Interfaces;

public interface IBinaryActuatorFactory
{
    IBinaryActuator Create(
        IDigitalOutput? outputOn,
        IDigitalOutput? outputOff,
        IDigitalInput? inputOn,
        IDigitalInput? inputOff
    );
}
