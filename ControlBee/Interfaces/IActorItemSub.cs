namespace ControlBee.Interfaces;

public interface IActorItemSub
{
    IActorInternal Actor { get; set; }
    string ItemName { get; set; }
    void UpdateSubItem();
}
