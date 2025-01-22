namespace ControlBee.Interfaces;

public interface IActorRegistry
{
    void Add(IActor actor);
    IActor Get(string actorName);
    string[] GetActorNames();
}
