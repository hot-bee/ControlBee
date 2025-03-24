using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

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
