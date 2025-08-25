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
        string severity,
        string? code = null,
        string? desc = null
    );

    DataTable ReadAll(string tableName);

    string? Read(string localName, string actorName, string itemPath);
    string[] GetLocalNames();
    void DeleteLocal(string localName);
}