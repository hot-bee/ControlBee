﻿using System.ComponentModel;
using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class AnalogInputPlaceholder : IPlaceholder, IAnalogInput
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

    public void RefreshCache()
    {
        throw new UnimplementedByDesignError();
    }

    public long Read()
    {
        throw new UnimplementedByDesignError();
    }
}
