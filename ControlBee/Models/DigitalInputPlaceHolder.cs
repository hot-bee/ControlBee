using System.ComponentModel;
using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalInputPlaceholder : IPlaceholder, IDigitalInput
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ItemPath { get; set; } = string.Empty;
    public string Name { get; } = string.Empty;
    public string Desc { get; } = string.Empty;

    public bool ProcessMessage(ActorItemMessage message)
    {
        throw new UnimplementedByDesignError();
    }

    public void UpdateSubItem()
    {
        throw new UnimplementedByDesignError();
    }

    public void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        throw new UnimplementedByDesignError();
    }

    public bool IsOn { get; } = false;
    public bool IsOff { get; } = false;

    public void WaitOn()
    {
        throw new UnimplementedByDesignError();
    }

    public void WaitOff()
    {
        throw new UnimplementedByDesignError();
    }

    public void WaitOn(int millisecondsTimeout)
    {
        throw new UnimplementedByDesignError();
    }

    public void WaitOff(int millisecondsTimeout)
    {
        throw new UnimplementedByDesignError();
    }
}
