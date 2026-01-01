using ControlBee.Interfaces;

namespace ControlBee.Models;

public class VariableActorItemMessage : ActorItemMessage
{
    public VariableActorItemMessage(IActor sender, string itemPath, object[] location, string name)
        : base(sender, itemPath, name)
    {
        Location = location;
    }

    public VariableActorItemMessage(
        Guid requestId,
        IActor sender,
        string itemPath,
        object[] location,
        string name,
        object? payload
    )
        : base(requestId, sender, itemPath, name, payload)
    {
        Location = location;
    }

    public VariableActorItemMessage(
        Guid requestId,
        IActor sender,
        string itemPath,
        object[] location,
        string name
    )
        : base(requestId, sender, itemPath, name)
    {
        Location = location;
    }

    public VariableActorItemMessage(
        IActor sender,
        string itemPath,
        object[] location,
        string name,
        object? payload
    )
        : base(sender, itemPath, name, payload)
    {
        Location = location;
    }

    public object[] Location { get; }
}
