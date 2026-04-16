using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class EmptyCounterFactory : ICounterFactory
{
    public static EmptyCounterFactory Instance = new();

    private EmptyCounterFactory() { }

    public ICounter Create()
    {
        throw new UnimplementedByDesignError();
    }
}
