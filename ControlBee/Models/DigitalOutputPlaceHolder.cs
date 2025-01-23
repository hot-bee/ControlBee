using System.ComponentModel;
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

    public bool On { get; set; }
    public bool Off { get; set; }
}
