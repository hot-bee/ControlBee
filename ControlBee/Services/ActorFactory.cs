using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Services;

public class ActorFactory(
    IAxisFactory axisFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IVariableManager variableManager,
    ITimeManager timeManager
)
{
    private readonly ActorRegistry? _actorRegistry;

    public ActorFactory(
        IAxisFactory axisFactory,
        IDigitalOutputFactory digitalOutputFactory,
        IVariableManager variableManager,
        ITimeManager timeManager,
        ActorRegistry actorRegistry
    )
        : this(axisFactory, digitalOutputFactory, variableManager, timeManager)
    {
        _actorRegistry = actorRegistry;
    }

    public T Create<T>(string actorName, params object?[]? args)
        where T : IActorInternal
    {
        if (!typeof(IActor).IsAssignableFrom(typeof(T)))
            throw new ApplicationException(
                "Cannot create this object. It must be derived from the 'Actor' class."
            );
        var actorConfig = new ActorConfig(
            actorName,
            axisFactory,
            digitalOutputFactory,
            variableManager,
            timeManager
        );
        var actorArgs = new List<object?> { actorConfig };
        if (args != null)
            actorArgs.AddRange(args);

        var actor = (T)Activator.CreateInstance(typeof(T), actorArgs.ToArray())!;
        actor.Init();
        _actorRegistry?.Add(actorName, actor);
        return actor;
    }
}
