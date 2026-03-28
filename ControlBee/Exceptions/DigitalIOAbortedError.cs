using ControlBeeAbstract.Exceptions;

namespace ControlBee.Exceptions;

public class DigitalIOAbortedError : SequenceError
{
    public DigitalIOAbortedError() { }

    public DigitalIOAbortedError(string message)
        : base(message) { }
}
