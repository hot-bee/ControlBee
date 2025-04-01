﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using ControlBeeAbstract.Exceptions;

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

    public void RefreshCache()
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
        throw new UnimplementedByDesignError();
    }

    public void On()
    {
        throw new UnimplementedByDesignError();
    }

    public void Off()
    {
        throw new UnimplementedByDesignError();
    }

    public bool? IsOn()
    {
        throw new UnimplementedByDesignError();
    }

    public bool? IsOff()
    {
        throw new UnimplementedByDesignError();
    }

    public bool IsCommandOn()
    {
        throw new UnimplementedByDesignError();
    }

    public bool IsCommandOff()
    {
        throw new UnimplementedByDesignError();
    }

    public void Wait()
    {
        throw new UnimplementedByDesignError();
    }

    public void OnAndWait()
    {
        throw new UnimplementedByDesignError();
    }

    public void OffAndWait()
    {
        throw new UnimplementedByDesignError();
    }
}
