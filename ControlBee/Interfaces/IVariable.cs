using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IVariable : IValueChanged
{
    object? ValueObject { get; }
    VariableScope Scope { get; }
    IActor Actor { get; set; }
    string GroupName { get; set; }
    string Uid { get; set; }
    string ToJson();
    void FromJson(string data);
    void UpdateSubItem();
}
