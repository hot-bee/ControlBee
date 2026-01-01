using ControlBee.Interfaces;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;
using log4net;
using Newtonsoft.Json.Linq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Vision(IDeviceManager deviceManager, ITimeManager timeManager)
    : DeviceChannel(deviceManager),
        IVision
{
    private const int Timeout = 5000;
    private static readonly ILog Logger = LogManager.GetLogger(nameof(Vision));

    public IDialog ConnectionError = new DialogPlaceholder();

    public Variable<int> PreDelay = new();
    public IDialog TimeoutError = new DialogPlaceholder();
    protected virtual IVisionDevice? VisionDevice => Device as IVisionDevice;

    public virtual void Trigger(int inspectionIndex, string? triggerId, Dict? options = null)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            if (PreDelay.Value > 0)
                Thread.Sleep(PreDelay.Value);
            VisionDevice.Trigger(Channel, inspectionIndex, triggerId, options);
        }
        catch (ConnectionError)
        {
            ConnectionError.Show();
            throw;
        }
    }

    public void Trigger(int inspectionIndex, Dict? options = null)
    {
        Trigger(inspectionIndex, null, options);
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

    public void SetLightOnOff(int inspectionIndex, bool on)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.SetLightOnOff(Channel, inspectionIndex, on);
        }
        catch (ConnectionError)
        {
            ConnectionError.Show();
            throw;
        }
    }

    public void SaveImage(string savePath, string? triggerId)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.SaveImage(Channel, savePath, triggerId);
        }
        catch (ConnectionError)
        {
            ConnectionError.Show();
            throw;
        }
    }

    public void SaveImage(string savePath)
    {
        SaveImage(savePath, null);
    }

    public void SetLightValue(int inspectionIndex, int lightChannel, double value)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.SetLightValue(Channel, inspectionIndex, lightChannel, value);
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

    public virtual void Wait(string triggerId, int timeout)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.Wait(triggerId, timeout);
        }
        catch (TimeoutError)
        {
            TimeoutError.Show();
            throw;
        }
    }

    public void WaitGrabEnd(int inspectionIndex, int timeout)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.WaitGrabEnd(Channel, inspectionIndex, timeout);
        }
        catch (TimeoutError)
        {
            TimeoutError.Show();
            throw;
        }
    }

    public void WaitExposureEnd(int inspectionIndex, int timeout)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return;
        }

        try
        {
            VisionDevice.WaitExposureEnd(Channel, inspectionIndex, timeout);
        }
        catch (TimeoutError)
        {
            TimeoutError.Show();
            throw;
        }
    }

    public virtual JObject? GetResult(int inspectionIndex)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return null;
        }

        return VisionDevice.GetResult(Channel, inspectionIndex);
    }

    public virtual JObject? GetResult(string triggerId)
    {
        if (VisionDevice == null)
        {
            Logger.Error($"VisionDevice is not set. ({ActorName}, {ItemPath})");
            return null;
        }

        return VisionDevice.GetResult(triggerId);
    }
}
