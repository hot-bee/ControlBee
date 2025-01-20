using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IDatabase
{
    void Write(
        VariableScope scope,
        string localName,
        string actorName,
        string itemPath,
        string value
    );
    string? Read(string localName, string actorName, string itemPath);
}
