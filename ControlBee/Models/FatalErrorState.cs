using ControlBee.Exceptions;

namespace ControlBee.Models;

public class FatalErrorState(Actor actor, SequenceError error) : ErrorState(actor, error)
{
    public override bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case StateEntryMessage.MessageName:
                Actor.SetStatus("Fatal", true);
                return true;
        }

        return false;
    }

    public override void Dispose()
    {
        Actor.SetStatus("Fatal", false);
    }
}
