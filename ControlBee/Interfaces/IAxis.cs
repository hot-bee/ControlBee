using ControlBee.Constants;
using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IAxis : IDeviceChannel
{
    void Enable();
    void Disable();
    bool IsAlarmed();
    bool IsEnabled();
    bool IsInitializing();
    bool IsNear(double position, double range);
    bool IsPosition(PositionComparisonType type, double position);
    void WaitForPosition(PositionComparisonType type, double position);
    bool IsMoving();
    void Move(double position);
    void MoveAndWait(double position);
    void SetSpeed(IVariable speedProfileVariable);
    void SetSpeed(SpeedProfile speedProfile);
    void VelocityMove(AxisDirection direction);
    void Stop();
    void SetPosition(double position, PositionType type = PositionType.CommandAndActual);
    void Wait();
    double GetPosition(PositionType type = PositionType.Command);
    bool GetSensorValue(AxisSensorType type);
    void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout);
    void Initialize();
    void SetInitializeAction(Action initializeAction);
    void Enable(bool value);
    SpeedProfile GetJogSpeed(JogSpeed jogSpeed);
}
