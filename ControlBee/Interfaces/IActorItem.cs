using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActorItem
{
    IActorInternal Actor { get; set; }
    string ItemName { get; set; }
    void ProcessMessage(Message message);
    void UpdateSubItem();
}
