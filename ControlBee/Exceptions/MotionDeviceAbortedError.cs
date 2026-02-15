using ControlBeeAbstract.Exceptions;

namespace ControlBee.Exceptions;

public class MotionDeviceAbortedError : SequenceError
{
    public MotionDeviceAbortedError() { }

    public MotionDeviceAbortedError(string message)
        : base(message) { }
}
