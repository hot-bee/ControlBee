using ControlBee.Exceptions;

namespace ControlBee.Models;

public class ErrorState(Actor actor, SequenceError error) : State<Actor>(actor)
{
    public SequenceError Error { get; } = error;

    public override bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case StateEntryMessage.MessageName:
                Actor.SetStatus("Error", true);
                return true;
        }

        return false;
    }

    public override void Dispose()
    {
        Actor.SetStatus("Error", false);
    }
}
