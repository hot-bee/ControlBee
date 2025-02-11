using ControlBee.Interfaces;

namespace ControlBee.Models;

public class TimeoutMessage(IActor sender) : Message(sender, MessageName)
{
    public const string MessageName = "_timeoutMessage";
}
