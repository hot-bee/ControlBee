using System.Text.Json.Serialization;
using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActorItemSub
{
    [JsonIgnore]
    IActorInternal Actor { get; set; }
    [JsonIgnore]
    string ItemPath { get; set; }
    void UpdateSubItem();
    void OnDeserialized();
    bool ProcessMessage(ActorItemMessage message);
}
