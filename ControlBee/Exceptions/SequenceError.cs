namespace ControlBee.Exceptions;

public class SequenceError : PlatformException
{
    public SequenceError() { }

    public SequenceError(string message)
        : base(message) { }
}
