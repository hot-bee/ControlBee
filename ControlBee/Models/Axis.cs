using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class Axis : IAxis
{
    public void Move(double position)
    {
        // TODO
    }

    public void SetSpeed(IVariable speedProfileVariable)
    {
        var profile = (SpeedProfile)speedProfileVariable.ValueObject!;
    }
}
