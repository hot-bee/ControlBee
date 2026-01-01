namespace ControlBee.Interfaces;

public interface IActorItemModifier
{
    string Name { get; set; }
    string Desc { get; set; }
    bool Visible { get; set; }
}
