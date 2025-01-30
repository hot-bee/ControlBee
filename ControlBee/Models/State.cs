using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class State<T>(T actor) : IState
    where T : Actor
{
    protected T Actor = actor;
    protected ITimeManager TimeManager => Actor.TimeManager;

    public abstract bool ProcessMessage(Message message);
}
