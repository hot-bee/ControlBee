using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class ActorItem : IActorItem
{
    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ActorName => Actor.Name;
    public string ItemPath { get; set; } = string.Empty;
    protected ITimeManager TimeManager => Actor.TimeManager;
    public abstract void ProcessMessage(ActorItemMessage message);
    public abstract void UpdateSubItem();
    public abstract void InjectProperties(IActorItemInjectionDataSource dataSource);
}
