namespace ControlBee.Exceptions;

public class PlatformException : ApplicationException
{
    public PlatformException() { }

    public PlatformException(string message)
        : base(message) { }
}
