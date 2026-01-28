using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models
{
    public class EmptyInitializeSequenceFactory : IInitializeSequenceFactory
    {
        public static EmptyInitializeSequenceFactory Instance =
            new EmptyInitializeSequenceFactory();

        private EmptyInitializeSequenceFactory() { }

        public IInitializeSequence Create(
            IAxis axis,
            Variable<SpeedProfile> initSpeed,
            Variable<Position1D> homePosition,
            AxisSensorType sensorType,
            AxisDirection direction
        )
        {
            throw new NotImplementedException();
        }
    }
}
