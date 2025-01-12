using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class State<T>(T actor) : IState
    where T : Actor
{
    protected T Actor = actor;

    public abstract IState ProcessMessage(Message message);
}
