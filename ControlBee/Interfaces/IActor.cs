using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActor
{
    string Name { get; }
    string Title { get; }
    void Send(Message message);
}
