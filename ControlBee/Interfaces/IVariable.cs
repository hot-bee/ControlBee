using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IVariable : IValueChanged
{
    object? ValueObject { get; }
    VariableScope Scope { get; }
    IActorInternal Actor { get; set; }
    string ActorName { get; }
    string ItemPath { get; set; }
    string ToJson();
    void FromJson(string data);
}
