using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IDatabase
{
    void Write(VariableScope scope, string localName, string groupName, string uid, string value);
    string? Read(string localName, string groupName, string uid);
}
