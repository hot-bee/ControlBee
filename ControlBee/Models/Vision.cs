using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;

namespace ControlBee.Models;

public class Vision(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager), IVision
{
    private const int Timeout = 5000;
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Vision));

    public IDialog ConnectionError = new DialogPlaceholder();

    public Variable<int> PreDelay = new();
    public IDialog TimeoutError = new DialogPlaceholder();
    protected virtual IVisionDevice? VisionDevice => Device as IVisionDevice;

    public virtual void Trigger(int inspectionIndex)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            if (PreDelay.Value > 0) Thread.Sleep(PreDelay.Value);
            VisionDevice.Trigger(Channel, inspectionIndex);
        }
        catch (ConnectionError)
        {
            ConnectionError.Show();
            throw;
        }
    }

    public void StartContinuous()
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        VisionDevice.StartContinuous(Channel);
    }

    public void StopContinuous()
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        VisionDevice.StopContinuous(Channel);
    }

    public bool IsContinuousMode()
    {
        throw new NotImplementedException();
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