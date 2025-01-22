using System.Collections.Generic;
using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class ActorRegistry : IActorRegistry
{
    private readonly Dictionary<string, IActor> _map = new();

    public void Add(IActor actor)
    {
        if (!_map.TryAdd(actor.Name, actor))
            throw new PlatformException(
                "The actor name is already registered to another actor. Please provide a different name."
            );
    }

    public IActor? Get(string actorName)
    {
        return _map.GetValueOrDefault(actorName);
    }

    public string[] GetActorNames()
    {
        return _map.Keys.ToArray();
    }
}
