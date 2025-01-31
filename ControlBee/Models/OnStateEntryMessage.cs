using ControlBee.Interfaces;

namespace ControlBee.Models;

public class OnStateEntryMessage(IActor sender) : Message(sender, MessageName)
{
    public const string MessageName = "_onStateEntry";
}
