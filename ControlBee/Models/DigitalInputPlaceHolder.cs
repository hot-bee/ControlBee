using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
