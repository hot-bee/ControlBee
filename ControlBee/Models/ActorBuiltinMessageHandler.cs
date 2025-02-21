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
                actor.PeerStatus[message.Sender] = message.DictPayload!;
                return true;
            case "_propertyRead":
            {
                var propertyPath = (string)message.Payload!;
                var value = actor.GetProperty(propertyPath);
                message.Sender.Send(new Message(message, actor, "_property", value));
                return true;
            }
            case "_jogStart":
            {
                var axisItemPath = (string)message.DictPayload!["AxisItemPath"]!;
                var direction = (AxisDirection)message.DictPayload!["Direction"]!;
                var jogSpeed = (JogSpeed)message.DictPayload!["JogSpeed"]!;
                var axis = (IAxis)actor.GetItem(axisItemPath)!;
                var speed = axis.GetJogSpeed(jogSpeed);
                if (speed == null)
                {
                    Logger.Error($"Couldn't get jog speed. ({actor.Name}, {axisItemPath})");
                    return false;
                }

                Logger.Debug("Continuous Jog Start");
                axis.SetSpeed(speed);
                axis.VelocityMove(direction);
                return true;
            }
            case "_jogStop":
            {
                Logger.Debug("Continuous Jog Stop");
                var axisItemPath = (string)message.DictPayload!["AxisItemPath"]!;
                var axis = (IAxis)actor.GetItem(axisItemPath)!;
                axis.Stop();
                return true;
            }
        }

        return false;
    }
}
