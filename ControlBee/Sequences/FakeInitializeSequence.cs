using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;

namespace ControlBee.Sequences;

public class FakeInitializeSequence(
    IAxis axis,
    Variable<SpeedProfile> initSpeed,
    Variable<Position1D> homePosition,
    AxisSensorType sensorType,
    AxisDirection direction
) : ActorItem, IInitializeSequence
{
    public void Run()
    {
        axis.Enable(true);
        axis.SetPosition(0.0);
        axis.SetSpeed(initSpeed);
        homePosition.Value.MoveAndWait();
    }
}
