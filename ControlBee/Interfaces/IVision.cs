using System.Text.Json.Nodes;

namespace ControlBee.Interfaces;

public interface IVision : IDeviceChannel
{
    void Trigger(int inspectionIndex, string? triggerId = null);
    void StartContinuous();
    void StopContinuous();
    bool IsContinuousMode();

    void Wait(int inspectionIndex, int timeout);
    void Wait(string triggerId, int timeout);
    void WaitGrabEnd(int inspectionIndex, int timeout);
    void WaitExposureEnd(int inspectionIndex, int timeout);
    JsonObject? GetResult(int inspectionIndex);
    JsonObject? GetResult(string triggerId);
}