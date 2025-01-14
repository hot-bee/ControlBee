using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IUiActor
{
    event EventHandler<Message>? MessageArrived;
}
