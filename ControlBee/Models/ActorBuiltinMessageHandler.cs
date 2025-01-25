using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorBuiltinMessageHandler(Actor actor)
{
    public void ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case "_initializeAxis":
            {
                var itemPath = (string)message.Payload!;
                var axis = (IAxis)actor.GetItem(itemPath)!;
                axis.Initialize();
                break;
            }
            case "_resetState":
            {
                actor.ResetState();
                break;
            }
        }
    }
}
