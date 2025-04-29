using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IDatabase
{
    void WriteVariables(
        VariableScope scope,
        string localName,
        string actorName,
        string itemPath,
        string value
    );

    void WriteEvents(
        string actorName,
        string code,
        string name,
        string desc,
        string severity
        );

    string? Read(string localName, string actorName, string itemPath);
}
