using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeVision(IDeviceManager deviceManager, ITimeManager timeManager)
    : Vision(deviceManager, timeManager)
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(FakeVision));

    public override void Trigger(int inspectionIndex)
    {
        Logger.Info($"Trigger {Channel}.");
    }

    public override void Wait(int inspectionIndex, int timeout)
    {
        Logger.Info($"Wait {Channel}.");
    }

    public override JsonObject? GetResult(int inspectionIndex)
    {
        return new JsonObject();
    }
}
