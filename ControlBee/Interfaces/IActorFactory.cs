namespace ControlBee.Interfaces;

public interface IActorFactory
{
    T Create<T>(string actorName, params object?[]? args)
        where T : IActorInternal;
}
