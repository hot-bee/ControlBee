using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IActor
{
    string ActorName { get; }
    void Send(Message message);
    void Init();
    IVariableManager VariableManager { get; }
    IPositionAxesMap PositionAxesMap { get; }
}
