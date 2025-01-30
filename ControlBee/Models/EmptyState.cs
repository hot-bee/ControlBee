using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyState(Actor actor) : State<Actor>(actor)
{
    public override bool ProcessMessage(Message message)
    {
        return false;
    }
}
