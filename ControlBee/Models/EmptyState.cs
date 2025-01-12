using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyState(Actor actor) : State<Actor>(actor)
{
    public override IState ProcessMessage(Message message)
    {
        return this;
    }
}
