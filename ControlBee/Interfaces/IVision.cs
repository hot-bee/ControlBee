using System.Text.Json.Nodes;

namespace ControlBee.Interfaces;

public interface IVision : IDeviceChannel
{
    void Trigger(int inspectionIndex);
    void Wait(int inspectionIndex, int timeout);
    JsonObject? GetResult(int inspectionIndex);
}
