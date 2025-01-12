namespace ControlBee.Interfaces;

public interface IActorItemSub
{
    IActor Actor { get; set; }
    string ItemName { get; set; }
    void UpdateSubItem();
}
