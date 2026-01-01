using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using log4net;
using Newtonsoft.Json.Linq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class FakeVision(IDeviceManager deviceManager, ITimeManager timeManager)
    : Vision(deviceManager, timeManager)
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(FakeVision));

    public override void Trigger(int inspectionIndex, string? triggerId, Dict? options = null)
    {
        Logger.Info($"Trigger {Channel}.");
    }

    public override void Wait(int inspectionIndex, int timeout)
    {
        Logger.Info($"Wait {Channel}.");
    }

    public override void Wait(string triggerId, int timeout)
    {
        Logger.Info($"Wait {Channel} ({triggerId}).");
    }

    public override JObject? GetResult(int inspectionIndex)
    {
        return new JObject();
    }

    public override JObject? GetResult(string triggerId)
    {
        return new JObject();
    }
}
