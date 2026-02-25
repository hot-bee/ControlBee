using ControlBeeAbstract.Constants;
using ControlBeeAbstract.Devices;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class FakeMotionDevice : IMotionDevice
{
    public readonly Dictionary<int, bool> AlarmSignal = new();

    public string DeviceName { get; set; } = "FakeDevice";

    public void Enable(int channel, bool value) { }

    public bool IsEnabled(int channel) => false;

    public bool IsAlarmed(int channel) => AlarmSignal.GetValueOrDefault(channel);

    public void ClearAlarm(int channel)
    {
        AlarmSignal[channel] = false;
    }

    public void OnBeforeInitialize(int channel) { }

    public bool IsMoving(int channel, PositionType type) => false;

    public void TrapezoidalMove(
        int channel,
        double position,
        double velocity,
        double acceleration,
        double deceleration
    ) { }

    public void SearchZPhase(int channel, double distance, double velocity, double acceleration) { }

    public void JerkRatioSCurveMove(
        int channel,
        double position,
        double velocity,
        double accel,
        double decel,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void JerkRatioSCurveRelativeMove(
        int channel,
        double distance,
        double velocity,
        double accel,
        double decel,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void VelocityMove(
        int channel,
        double velocity,
        double acceleration,
        double deceleration,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void Stop(int channel) { }

    public void EStop(int channel) { }

    public void SetSoftwareLimit(
        int channel,
        bool enable,
        double negativeLimit,
        double positiveLimit
    ) { }

    public void SetCommandPosition(int channel, double position) { }

    public void SetActualPosition(int channel, double position) { }

    public void SetTorque(int channel, double torque) { }

    public void Wait(int channel, PositionType type) { }

    public void Wait(int channel, int millisecondsTimeout, PositionType type) { }

    public double GetCommandPosition(int channel) => 0;

    public double GetActualPosition(int channel) => 0;

    public double GetCommandVelocity(int channel) => 0;

    public double GetActualVelocity(int channel) => 0;

    public bool GetHomeSensor(int channel) => false;

    public bool GetPositiveLimitSensor(int channel) => false;

    public bool GetNegativeLimitSensor(int channel) => false;

    public void BuiltinInitialize(int channel) { }

    public void SpecialCommand(int channel, Dict data) { }

    public void PrepareJerkRatioSCurveMove(
        int channel,
        double position,
        double velocity,
        double accel,
        double decel,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void PrepareJerkRatioSCurveRelativeMove(
        int channel,
        double distance,
        double velocity,
        double accel,
        double decel,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void ExecutePreparedMoves() { }

    public void ExecutePreparedMovesWhenCountIs(int count) { }

    public void ClearPreparedMoves() { }

    public void WaitUntilMoveQueueEmpty(int millisecondsTimeout) { }

    public int GetPreparedMoveCount() => 0;

    public bool IsOpenLoop(int channel) => false;

    public void SetCommandAndActualPosition(int channel, double position) { }

    public void SetAcceleration(int channel, double acceleration) { }

    public void SetDeceleration(int channel, double deceleration) { }

    public void SetAccelJerk(int channel, double jerk) { }

    public void SetDecelJerk(int channel, double jerk) { }

    public void StartECam(
        int channel,
        int masterChannel,
        int masterSource,
        double[] masterPositions,
        double[] slavePositions
    ) { }

    public void StopECam(int channel) { }

    public bool IsECamEnabled(int channel) => false;

    public void JerkRatioSCurveMultiMove(JerkRatioSCurveMoveParameter[] parameters) { }

    public void InterpolateMove(
        (int channel, double position)[] channelPositions,
        double velocity,
        double acceleration,
        double deceleration,
        double jerkRatio,
        double jerkRatioDecel
    ) { }

    public void SetSyncGearRatio(
        int channel,
        int masterChannel,
        double slaveRatio,
        double masterRatio,
        double velocity,
        double acceleration,
        double deceleration,
        double jerkRatio
    ) { }

    public void ResolveSync(int channel) { }

    public void Init(string deviceName, Dict config)
    {
        DeviceName = deviceName;
    }

    public object? GetInitArgument(string key) => null;

    public void Dispose() { }
}
