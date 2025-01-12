using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(IVariableManager variableManager)
{
    public T Create<T>(string actorName, params object?[]? args)
        where T : IActor
    {
        if (!typeof(IActor).IsAssignableFrom(typeof(T)))
            throw new ApplicationException(
                "Cannot create this object. It must be derived from the 'Actor' class."
            );
        var actorConfig = new ActorConfig(actorName, variableManager);
        var actorArgs = new List<object?> { actorConfig };
        if (args != null)
            actorArgs.AddRange(args);

        var actor = (T)Activator.CreateInstance(typeof(T), actorArgs.ToArray())!;
        actor.Init();
        return actor;
    }
}
