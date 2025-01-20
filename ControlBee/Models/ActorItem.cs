using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class ActorItem : IActorItem
{
    public IActorInternal Actor { get; set; } = Models.Actor.Empty;
    public string ActorName => Actor.Name;
    public string ItemPath { get; set; } = string.Empty;
    protected ITimeManager TimeManager => Actor.TimeManager;
    public abstract void ProcessMessage(Message message);
    public abstract void UpdateSubItem();
}
