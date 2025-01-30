using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DroppedMessage(Guid requestId, IActor sender) : Message(requestId, sender, MessageName)
{
    public const string MessageName = "_droppedMessage";
}
