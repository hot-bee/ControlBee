using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyDigitalInputFactory : IDigitalInputFactory
{
    public static EmptyDigitalInputFactory Instance = new();

    private EmptyDigitalInputFactory() { }

    public IDigitalInput Create()
    {
        throw new UnimplementedByDesignError();
    }
}
