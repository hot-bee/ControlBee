namespace ControlBee.Interfaces;

public interface IActorItemSub
{
    IActorInternal Actor { get; set; }
    string ItemPath { get; set; }
    void UpdateSubItem();
    void OnDeserialized();
}
