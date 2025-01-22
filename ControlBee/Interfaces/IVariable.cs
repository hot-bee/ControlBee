using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IVariable : IActorItem, IValueChanged
{
    object? ValueObject { get; }
    VariableScope Scope { get; }
    string ActorName { get; }
    string ToJson();
    void FromJson(string data);
}
