using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyDigitalOutputFactory : IDigitalOutputFactory
{
    public static EmptyDigitalOutputFactory Instance = new();

    private EmptyDigitalOutputFactory() { }

    public IDigitalOutput Create()
    {
        throw new UnimplementedByDesignError();
    }
}
