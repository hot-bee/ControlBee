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

    void Write(
        string actorName,
        string code,
        string name,
        string desc,
        string severity
        );

    string? Read(string localName, string actorName, string itemPath);
}
