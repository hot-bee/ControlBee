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
