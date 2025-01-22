using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActorItem
{
    IActorInternal Actor { get; set; }
    string ItemPath { get; set; }
    string Name { get; }
    string Desc { get; }
    void ProcessMessage(ActorItemMessage message);
    void UpdateSubItem();
    void InjectProperties(IActorItemInjectionDataSource dataSource);
}
