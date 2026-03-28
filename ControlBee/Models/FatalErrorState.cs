using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class FatalErrorState<T>(T actor, SequenceError error) : ErrorState<T>(actor, error)
    where T : Actor
{
    public override bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case StateEntryMessage.MessageName:
                Actor.SetStatus("_error", true);
                Actor.SetStatus("_fatal", true);
                return true;
            case "_resetError":
                Actor.State = Actor.CreateInitialState();
                return true;
        }

        return false;
    }

    public override void Dispose()
    {
        Actor.SetStatus("_error", false);
        Actor.SetStatus("_fatal", false);
    }
}
