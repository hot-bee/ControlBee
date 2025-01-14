namespace ControlBee.Exceptions;

public class ValueError : PlatformException
{
    public ValueError() { }

    public ValueError(string message)
        : base(message) { }
}
