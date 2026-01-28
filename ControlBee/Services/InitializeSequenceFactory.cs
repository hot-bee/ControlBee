using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;

namespace ControlBee.Services;

public class InitializeSequenceFactory(ISystemConfigurations systemConfigurations)
    : IInitializeSequenceFactory
{
    public IInitializeSequence Create(
        IAxis axis,
        Variable<SpeedProfile> initSpeed,
        Variable<Position1D> homePosition,
        AxisSensorType sensorType,
        AxisDirection direction
    )
    {
        if (systemConfigurations.FakeMode)
            return new FakeInitializeSequence(axis, initSpeed, homePosition, sensorType, direction);
        return new InitializeSequence(axis, initSpeed, homePosition, sensorType, direction);
    }
}
