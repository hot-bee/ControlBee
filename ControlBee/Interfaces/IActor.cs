using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActor
{
    string Name { get; }
    string Title { get; }
    Guid Send(Message message);
    (string itemPath, Type type)[] GetItems();
}
