using System.Data;
using ControlBee.Models;
using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IDatabase
{
    int WriteVariables(
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

    DataTable ReadAll(string tableName, QueryOptions? options = null);

    (int id, string value)? Read(string localName, string actorName, string itemPath);
    Dictionary<
        (string localName, string actorName, string itemPath),
        (int id, string value)
    > ReadAllVariables(string localName);
    string[] GetLocalNames();
    void DeleteLocal(string localName);
    void RenameLocalName(string sourceLocalName, string targetLocalName);
    void WriteVariableChange(IVariable variable, ValueChangedArgs valueChangedArgs);
    DataTable ReadVariableChanges(QueryOptions? options = null);
    object GetConnection();
    DataTable ReadLatestVariableChanges();
}
