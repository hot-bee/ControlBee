using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IVariable : IValueChanged
{
    VariableScope Scope { get; }
    string GroupName { get; }
    string Uid { get; }
    string ToJson();
    void FromJson(string data);
}
