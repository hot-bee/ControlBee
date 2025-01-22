using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyAxisFactory : IAxisFactory
{
    public static IAxisFactory Instance = new EmptyAxisFactory();

    private EmptyAxisFactory() { }

    public IAxis Create()
    {
        throw new UnimplementedByDesignError();
    }
}
