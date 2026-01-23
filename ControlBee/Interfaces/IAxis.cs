using ControlBee.Constants;
using ControlBee.Sequences;
using ControlBee.Variables;
using ControlBeeAbstract.Constants;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Interfaces;

public interface IAxis : IDeviceChannel
{
    InitializeSequence InitializeSequence { get; }
    double ResolutionValue { get; }
    void Enable();
    void Disable();
    bool IsAlarmed();
    void ClearAlarm();
    bool IsEnabled();
    bool IsInitializing();
    bool IsInitialized();
    void OnBeforeInitialize();
    bool IsNear(double position, double range);
    bool WaitNear(double position, double range);
    bool IsPosition(PositionComparisonType type, double position);
    bool WaitForPosition(PositionComparisonType type, double position);
    bool IsFar(double position, double range);
    bool WaitFar(double position, double range);
    bool IsMoving(PositionType type = PositionType.CommandAndActual);
    void Move(double position);
    void Move(double position, bool @override);
    void MoveAndWait(double position, PositionType type = PositionType.CommandAndActual);
    void SetSpeed(IVariable speedProfileVariable);
    void SetSpeed(SpeedProfile speedProfile);
    void VelocityMove(AxisDirection direction);
    void VelocityMove(AxisDirection direction, bool @override);
    void Stop();
    void EStop();
    void ClearPosition(PositionType type = PositionType.CommandAndActual);
    void SetPosition(double position, PositionType type = PositionType.CommandAndActual);
    void SetTorque(double torque);
    void Wait(PositionType type = PositionType.CommandAndActual);
    double GetPosition(PositionType type = PositionType.Command);
    double GetVelocity(VelocityType type);
    bool GetSensorValue(AxisSensorType type);
    bool GetSensorValueOrTrue(AxisSensorType type);
    bool GetSensorValueOrFalse(AxisSensorType type);
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
    bool IsVelocityMoving();
    void BuiltinInitialize();
    void SpecialCommand(Dict data);
    void SetSoftwareLimit(bool enable, double negativeLimit, double positiveLimit);
    bool GetUseSoftwareLimit();
    double GetPositiveSoftwareLimitPosition();
    double GetNegativeSoftwareLimitPosition();
    void PrepareMove(double position);
    void PrepareRelativeMove(double distance);
    void ExecutePreparedMoves();
    void ExecutePreparedMovesWhenCountIs(int count);
    void ClearPreparedMoves();
    void WaitUntilMoveQueueEmpty(int millisecondsTimeout = 10000);
    int GetPreparedMoveCount();
}
