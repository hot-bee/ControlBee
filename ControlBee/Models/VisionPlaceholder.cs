using System.ComponentModel;
using System.Text.Json.Nodes;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;

namespace ControlBee.Models;

public class VisionPlaceholder : IPlaceholder, IVision
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IActorInternal Actor { get; set; }
    public string ItemPath { get; set; }
    public string Name { get; }
    public string Desc { get; }

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

    public void RefreshCache()
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

    public void Trigger(int inspectionIndex)
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

    public void Wait(int inspectionIndex, int timeout)
    {
        throw new NotImplementedException();
    }

    public JsonObject? GetResult(int inspectionIndex)
    {
        throw new NotImplementedException();
    }
}
