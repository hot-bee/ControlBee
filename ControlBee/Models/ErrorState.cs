using ControlBee.Exceptions;

namespace ControlBee.Models;

public class ErrorState<T>(T actor, SequenceError error) : State<T>(actor)
    where T : Actor
{
    public SequenceError Error { get; } = error;

    public override bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case StateEntryMessage.MessageName:
                Actor.SetStatus("_error", true);
                return true;
            case "_resetError":
                Actor.State = Actor.CreateIdleState();
                return true;
        }

        return false;
    }

    public override void Dispose()
    {
        Actor.SetStatus("_error", false);
    }
}
