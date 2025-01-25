using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IInitializeSequenceFactory
{
    IInitializeSequence Create(
        IAxis axis,
        Variable<SpeedProfile> homingSpeed,
        Variable<Position1D> homePosition
    );

    IInitializeSequence Create(IAxis axis, SpeedProfile homingSpeed, Position1D homePosition);
}
