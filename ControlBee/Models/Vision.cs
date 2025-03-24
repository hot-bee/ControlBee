using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
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

    public IDialog ConnectionError = new DialogPlaceholder();
    public IDialog TimeoutError = new DialogPlaceholder();

    public virtual void Trigger(int inspectionIndex)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.Trigger(Channel, inspectionIndex);
        }
        catch (ConnectionError)
        {
            ConnectionError.Show();
            throw;
        }
    }

    public virtual void Wait(int inspectionIndex, int timeout)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.Wait(Channel, inspectionIndex, timeout);
        }
        catch (TimeoutError)
        {
            TimeoutError.Show();
            throw;
        }
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
