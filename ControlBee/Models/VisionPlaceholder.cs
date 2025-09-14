using System.ComponentModel;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using Newtonsoft.Json.Linq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class VisionPlaceholder : IPlaceholder, IVision
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IActorInternal Actor { get; set; }
    public string ItemPath { get; set; }
    public string Name { get; }
    public string Desc { get; }
    public bool Visible { get; }

    public bool ProcessMessage(ActorItemMessage message)
    {
        throw new NotImplementedException();
    }

    public void UpdateSubItem()
    {
        throw new NotImplementedException();
    }

    public void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        throw new NotImplementedException();
    }

    public void Init()
    {
        throw new NotImplementedException();
    }

    public void PostInit()
    {
        throw new NotImplementedException();
    }

    public void RefreshCache(bool alwaysUpdate = false)
    {
        throw new NotImplementedException();
    }

    public IDevice? GetDevice()
    {
        throw new NotImplementedException();
    }

    public int GetChannel()
    {
        throw new NotImplementedException();
    }

    public void Trigger(int inspectionIndex, string? triggerId, Dict? options = null)
    {
        throw new NotImplementedException();
    }

    public void Trigger(int inspectionIndex, Dict? options = null)
    {
        throw new NotImplementedException();
    }

    public void StartContinuous()
    {
        throw new NotImplementedException();
    }

    public void StopContinuous()
    {
        throw new NotImplementedException();
    }

    public bool IsContinuousMode()
    {
        throw new NotImplementedException();
    }

    public void SetLightOnOff(int inspectionIndex, bool on)
    {
        // pass
    }

    public void Wait(int inspectionIndex, int timeout)
    {
        throw new NotImplementedException();
    }

    public void Wait(string triggerId, int timeout)
    {
        throw new NotImplementedException();
    }

    public void WaitGrabEnd(int inspectionIndex, int timeout)
    {
        throw new NotImplementedException();
    }

    public void WaitExposureEnd(int inspectionIndex, int timeout)
    {
        throw new NotImplementedException();
    }

    public JObject? GetResult(int inspectionIndex)
    {
        throw new NotImplementedException();
    }

    public JObject? GetResult(string triggerId)
    {
        throw new NotImplementedException();
    }
}
