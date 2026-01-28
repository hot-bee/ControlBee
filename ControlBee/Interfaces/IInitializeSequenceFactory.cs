using ControlBee.Constants;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;

namespace ControlBee.Interfaces;

public interface IInitializeSequenceFactory
{
    IInitializeSequence Create(
        IAxis axis,
        Variable<SpeedProfile> initSpeed,
        Variable<Position1D> homePosition,
        AxisSensorType sensorType,
        AxisDirection direction
    );
}
