using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DroppedMessage(Guid requestId, IActor sender)
    : Message(requestId, sender, "_droppedMessage");
