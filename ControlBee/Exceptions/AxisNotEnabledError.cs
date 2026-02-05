using ControlBeeAbstract.Exceptions;

namespace ControlBee.Exceptions;

public class AxisNotEnabledError : FatalSequenceError
{
    public AxisNotEnabledError() { }

    public AxisNotEnabledError(string message)
        : base(message) { }
}
