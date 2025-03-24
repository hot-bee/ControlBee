using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Vision(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IVision
{
    private const int Timeout = 5000;
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Vision));
    protected virtual IVisionDevice? VisionDevice => Device as IVisionDevice;

    public virtual void Trigger(int inspectionIndex)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        VisionDevice.Trigger(Channel, inspectionIndex);
    }

    public virtual void Wait(int inspectionIndex, int timeout)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        VisionDevice.Wait(Channel, inspectionIndex, timeout);
    }

    public virtual JsonObject? GetResult(int inspectionIndex)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return null;
        }

        return VisionDevice.GetResult(Channel, inspectionIndex);
    }
}
