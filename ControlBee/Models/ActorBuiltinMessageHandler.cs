using System.Reflection;
using ControlBee.Constants;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class ActorBuiltinMessageHandler(Actor actor)
{
    private static readonly ILog Logger = LogManager.GetLogger(
        MethodBase.GetCurrentMethod()!.DeclaringType!
    );

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
                var peerStatus = actor.PeerStatus[message.Sender];
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
