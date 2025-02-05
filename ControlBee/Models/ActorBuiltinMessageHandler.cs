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
            case "_status":
                actor.PeerStatus[message.Sender] = message.DictPayload!;
                return true;
            case "_propertyRead":
            {
                var propertyPath = (string)message.Payload!;
                var value = actor.GetProperty(propertyPath);
                message.Sender.Send(new Message(message, actor, "_property", value));
                return true;
            }
        }
        return false;
    }
}
