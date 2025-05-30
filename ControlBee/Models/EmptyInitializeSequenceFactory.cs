﻿using ControlBee.Interfaces;
using ControlBee.Variables;
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
            Variable<SpeedProfile> homingSpeed,
            Variable<Position1D> homePosition
        )
        {
            throw new UnimplementedByDesignError();
        }

        public IInitializeSequence Create(
            IAxis axis,
            SpeedProfile homingSpeed,
            Position1D homePosition
        )
        {
            throw new UnimplementedByDesignError();
        }
    }
}
