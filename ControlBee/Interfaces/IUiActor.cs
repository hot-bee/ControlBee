using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IUiActor : IActor
{
    event EventHandler<Message>? MessageArrived;
}
