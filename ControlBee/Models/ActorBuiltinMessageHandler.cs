using System.Reflection;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class ActorBuiltinMessageHandler(Actor actor)
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

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
            {
                if (!actor.PeerStatus.TryGetValue(message.Sender, out var peerStatus))
                {
                    peerStatus = [];
                    actor.PeerStatus[message.Sender] = peerStatus;
                }

                foreach (var (key, value) in message.DictPayload!)
                    peerStatus[key] = value;
                return true;
            }
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
