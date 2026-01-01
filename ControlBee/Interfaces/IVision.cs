using Newtonsoft.Json.Linq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Interfaces;

public interface IVision : IDeviceChannel
{
    void Trigger(int inspectionIndex, string? triggerId, Dict? options = null);
    void Trigger(int inspectionIndex, Dict? options = null);
    void StartContinuous();
    void StopContinuous();
    bool IsContinuousMode();
    void SetLightOnOff(int inspectionIndex, bool on);

    void Wait(int inspectionIndex, int timeout);
    void Wait(string triggerId, int timeout);
    void WaitGrabEnd(int inspectionIndex, int timeout);
    void WaitExposureEnd(int inspectionIndex, int timeout);
    JObject? GetResult(int inspectionIndex);
    JObject? GetResult(string triggerId);
    void SaveImage(string savePath, string? triggerId);
    void SaveImage(string savePath);
    void SetLightValue(int inspectionIndex, int lightChannel, double value);
}
