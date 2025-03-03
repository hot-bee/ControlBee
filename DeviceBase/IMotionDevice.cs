namespace DeviceBase;

public interface IMotionDevice : IDevice
{
    void Enable(int channel, bool value);
    bool IsEnabled(int channel);
    void TrapezoidalMove(
        int channel,
        int position,
        int velocity,
        int acceleration,
        int deceleration
    );
    void Wait(int channel);
    void Wait(int channel, int timeout);
    bool IsMoving(int channel);
    void SetCommandPosition(double position);
    void SetActualPosition(double position);
}
