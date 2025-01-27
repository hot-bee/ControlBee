using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalOutputPlaceholder : IPlaceholder, IDigitalOutput
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

    public void SetOn(bool on)
    {
        throw new NotImplementedException();
    }

    public void On()
    {
        throw new NotImplementedException();
    }

    public void Off()
    {
        throw new NotImplementedException();
    }

    public bool? IsOn()
    {
        throw new NotImplementedException();
    }

    public bool? IsOff()
    {
        throw new NotImplementedException();
    }

    public bool IsCommandOn()
    {
        throw new NotImplementedException();
    }

    public bool IsCommandOff()
    {
        throw new NotImplementedException();
    }

    public void Wait()
    {
        throw new NotImplementedException();
    }

    public void OnAndWait()
    {
        throw new NotImplementedException();
    }

    public void OffAndWait()
    {
        throw new NotImplementedException();
    }
}
