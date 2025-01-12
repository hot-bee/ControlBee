using ControlBee.Interfaces;

namespace ControlBee.Models;

public class Message(IActor sender, object payload)
{
    public static readonly Message Empty = new Message(Actor.Empty, 0);
    public IActor Sender { get; } = sender;
    public object Payload { get; } = payload;
}
