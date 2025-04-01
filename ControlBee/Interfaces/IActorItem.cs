using System.ComponentModel;
using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActorItem : INotifyPropertyChanged
{
    IActorInternal Actor { get; set; }
    string ItemPath { get; set; }
    string Name { get; }
    string Desc { get; }
    bool ProcessMessage(ActorItemMessage message);
    void UpdateSubItem();
    void InjectProperties(ISystemPropertiesDataSource dataSource);
    void Init();
    void PostInit();
}
