using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorBuiltinMessageHandler(IActor actor)
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
        }
    }
}
