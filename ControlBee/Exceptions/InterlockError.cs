namespace ControlBee.Exceptions;

public class InterlockError : SequenceError
{
    public InterlockError() { }

    public InterlockError(string message)
        : base(message) { }
}
