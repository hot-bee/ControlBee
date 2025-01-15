namespace ControlBee.Exceptions;

public class TimeoutError : PlatformException
{
    public TimeoutError() { }

    public TimeoutError(string message)
        : base(message) { }
}
