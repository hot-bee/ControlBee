using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyAxisFactory : IAxisFactory
{
    public IAxis Create()
    {
        throw new UnimplementedByDesignError();
    }
}
