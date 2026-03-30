using ControlBeeAbstract.Exceptions;

namespace ControlBee.Exceptions;

public class DeviceAbortedError : SequenceError
{
    public DeviceAbortedError() { }

    public DeviceAbortedError(string message)
        : base(message) { }
}
