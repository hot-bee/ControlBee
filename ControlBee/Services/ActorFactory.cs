using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(
    IAxisFactory axisFactory,
    IVariableManager variableManager,
    ITimeManager timeManager
)
{
    private readonly Dictionary<string, IActor> _map = new();

    public T Create<T>(string actorName, params object?[]? args)
        where T : IActorInternal
    {
        if (!typeof(IActor).IsAssignableFrom(typeof(T)))
            throw new ApplicationException(
                "Cannot create this object. It must be derived from the 'Actor' class."
            );
        var actorConfig = new ActorConfig(actorName, axisFactory, variableManager, timeManager);
        var actorArgs = new List<object?> { actorConfig };
        if (args != null)
            actorArgs.AddRange(args);

        var actor = (T)Activator.CreateInstance(typeof(T), actorArgs.ToArray())!;
        actor.Init();
        _map[actorName] = actor;
        return actor;
    }

    public IActor Get(string actorName)
    {
        return _map[actorName];
    }
}
