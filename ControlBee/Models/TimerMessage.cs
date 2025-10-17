using ControlBee.Interfaces;

namespace ControlBee.Models;

public class TimerMessage(IActor sender) : Message(sender, MessageName)
{
    public const string MessageName = "_timerMessage";
}
