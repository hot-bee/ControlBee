using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Sequences;

public class FakeInitializeSequence(IAxis axis, SpeedProfile homingSpeed, Position1D homePosition)
    : IInitializeSequence
{
    public void Run()
    {
        axis.SetPosition(0.0);
        axis.SetSpeed(homingSpeed);
        homePosition.MoveAndWait();
    }
}
