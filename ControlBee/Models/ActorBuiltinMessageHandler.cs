using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorBuiltinMessageHandler(Actor actor)
{
    public bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case "_initializeAxis":
            {
                var itemPath = (string)message.Payload!;
                var axis = (IAxis)actor.GetItem(itemPath)!;
                axis.Initialize();
                return true;
            }
            case "_resetState":
            {
                actor.ResetState();
                return true;
            }
        }

        return false;
    }
}
