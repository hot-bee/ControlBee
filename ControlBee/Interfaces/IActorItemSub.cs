using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActorItemSub
{
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    IActorInternal Actor { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    string ItemPath { get; set; }
    void UpdateSubItem();
    void OnDeserialized();
    bool ProcessMessage(ActorItemMessage message);
}
