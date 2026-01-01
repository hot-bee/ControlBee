namespace ControlBee.Interfaces;

public interface IDialog : IActorItem
{
    Guid Show();
    Guid Show(string[] actionButtons);
    Guid Show(string desc);
    void Close();
}
