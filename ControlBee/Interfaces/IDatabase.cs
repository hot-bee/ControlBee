using System.Data;
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
        string name,
        string? code = null,
        string? desc = null,
        string? severity = null
        );

    DataTable ReadAll(string tableName);

    string? Read(string localName, string actorName, string itemPath);
}
