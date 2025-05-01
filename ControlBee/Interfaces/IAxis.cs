using ControlBee.Constants;
using ControlBee.Sequences;
using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IAxis : IDeviceChannel
{
    void Enable();
    void Disable();
    bool IsAlarmed();
    void ClearAlarm();
    bool IsEnabled();
    bool IsInitializing();
    bool IsNear(double position, double range);
    bool IsPosition(PositionComparisonType type, double position);
    void WaitForPosition(PositionComparisonType type, double position);
    bool IsFar(double position, double range);
    void WaitFar(double position, double range);
    bool IsMoving();
    void Move(double position);
    void Move(double position, bool @override);
    void MoveAndWait(double position);
    void SetSpeed(IVariable speedProfileVariable);
    void SetSpeed(SpeedProfile speedProfile);
    void VelocityMove(AxisDirection direction);
    void VelocityMove(AxisDirection direction, bool @override);
    void Stop();
    void EStop();
    void SetPosition(double position, PositionType type = PositionType.CommandAndActual);
    void SetTorque(double torque);
    void Wait();
    double GetPosition(PositionType type = PositionType.Command);
    double GetVelocity(VelocityType type);
    bool GetSensorValue(AxisSensorType type);
    void WaitSensor(AxisSensorType type, bool waitingValue, int millisecondsTimeout);
    void Initialize();
    void SetInitializeAction(Action initializeAction);
    void Enable(bool value);
    SpeedProfile GetJogSpeed(JogSpeedLevel jogSpeedLevel);
    void RelativeMove(double distance);
    void RelativeMoveAndWait(double distance);
    SpeedProfile GetNormalSpeed();
    SpeedProfile GetInitSpeed();
    Position1D GetInitPos();
    void SearchZPhase(double distance);
    InitializeSequence InitializeSequence { get; }

}