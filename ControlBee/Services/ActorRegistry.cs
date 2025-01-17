using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class ActorRegistry
{
    private readonly Dictionary<string, IActor> _map = new();

    public void Add(string actorName, IActor actor)
    {
        if (!_map.TryAdd(actorName, actor))
            throw new PlatformException(
                "The actor name is already registered to another actor. Please provide a different name."
            );
    }

    public IActor Get(string actorName)
    {
        return _map[actorName];
    }

    public string[] GetActorNames()
    {
        return _map.Keys.ToArray();
    }
}
