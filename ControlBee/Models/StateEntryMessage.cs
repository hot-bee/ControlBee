using ControlBee.Interfaces;

namespace ControlBee.Models;

public class StateEntryMessage(IActor sender) : Message(sender, MessageName)
{
    public const string MessageName = "_stateEntry";
}
