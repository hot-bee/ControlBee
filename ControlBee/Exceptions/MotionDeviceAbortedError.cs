namespace ControlBee.Exceptions;

public class MotionDeviceAbortedError : Exception
{
    public MotionDeviceAbortedError() { }

    public MotionDeviceAbortedError(string message)
        : base(message) { }
}
