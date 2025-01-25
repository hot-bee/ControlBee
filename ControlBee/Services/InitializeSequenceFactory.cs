using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Variables;

namespace ControlBee.Services;

public class InitializeSequenceFactory(SystemConfigurations systemConfigurations)
{
    public IInitializeSequence Create(
        IAxis axis,
        Variable<SpeedProfile> homingSpeed,
        Variable<Position1D> homePosition
    )
    {
        return Create(axis, homingSpeed.Value, homePosition.Value);
    }

    public IInitializeSequence Create(IAxis axis, SpeedProfile homingSpeed, Position1D homePosition)
    {
        if (systemConfigurations.FakeMode)
            return new FakeInitializeSequence();
        return new InitializeSequence(axis, homingSpeed, homePosition);
    }
}
