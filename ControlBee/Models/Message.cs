using ControlBee.Interfaces;

namespace ControlBee.Models;

public class Message(IActor sender, string name, object? payload)
{
    public static readonly Message Empty = new(Actor.Empty, "_empty");

    public Message(IActor sender, string name)
        : this(sender, name, null) { }

    public IActor Sender { get; } = sender;
    public string Name { get; } = name;
    public object? Payload { get; } = payload;
}
