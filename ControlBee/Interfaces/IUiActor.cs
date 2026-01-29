using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IUiActor : IActor
{
    object? GetPeerStatus(IActor actor, string keyName);
    object? GetPeerStatus(string actorName, string keyName);
    event EventHandler<Message>? MessageArrived;
}
