using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

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
