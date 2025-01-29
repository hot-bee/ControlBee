namespace ControlBee.Exceptions;

public class InterlockError : PlatformException
{
    public InterlockError() { }

    public InterlockError(string message)
        : base(message) { }
}
