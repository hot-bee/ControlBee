using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(IVariableManager variableManager)
{
    public T Create<T>(string actorName)
        where T : IActor
    {
        if (!typeof(IActor).IsAssignableFrom(typeof(T)))
            throw new ApplicationException(
                "Cannot create this object. It must be derived from the 'Actor' class."
            );
        var actorConfig = new ActorConfig(actorName, variableManager);
        var actor = (T)Activator.CreateInstance(typeof(T), actorConfig)!;
        return actor;
    }
}
