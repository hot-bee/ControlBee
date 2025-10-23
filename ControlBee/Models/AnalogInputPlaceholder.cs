using System.ComponentModel;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class AnalogInputPlaceholder : IPlaceholder, IAnalogInput
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ItemPath { get; set; } = string.Empty;
    public string Name { get; } = string.Empty;
    public string Desc { get; } = string.Empty;
    public bool Visible { get; set; }

    public bool ProcessMessage(ActorItemMessage message)
    {
        throw new UnimplementedByDesignError();
    }

    public void UpdateSubItem()
    {
        throw new UnimplementedByDesignError();
    }

    public void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        throw new UnimplementedByDesignError();
    }

    public void Init()
    {
        // Empty
    }

    public void PostInit()
    {
        throw new NotImplementedException();
    }

    public void RefreshCache(bool alwaysUpdate = false)
    {
        throw new UnimplementedByDesignError();
    }

    public IDevice? GetDevice()
    {
        throw new NotImplementedException();
    }

    public int GetChannel()
    {
        throw new NotImplementedException();
    }

    public long Read()
    {
        throw new UnimplementedByDesignError();
    }
}
