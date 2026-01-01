using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorItemMessage : Message
{
    public ActorItemMessage(IActor sender, string itemPath, string name)
        : base(sender, name)
    {
        ItemPath = itemPath;
    }

    public ActorItemMessage(
        Guid requestId,
        IActor sender,
        string itemPath,
        string name,
        object? payload
    )
        : base(requestId, sender, name, payload)
    {
        ItemPath = itemPath;
    }

    public ActorItemMessage(Guid requestId, IActor sender, string itemPath, string name)
        : base(requestId, sender, name)
    {
        ItemPath = itemPath;
    }

    public ActorItemMessage(IActor sender, string itemPath, string name, object? payload)
        : base(sender, name, payload)
    {
        ItemPath = itemPath;
    }

    public string ItemPath { get; }
}
