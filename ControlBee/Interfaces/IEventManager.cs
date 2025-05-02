using System.Data;

namespace ControlBee.Interfaces;

public interface IEventManager
{
    public void Write(
        string actorName,
        string name,
        string? code = null,
        string? desc = null,
        string? severity = null
    );

    public DataTable ReadAll(string tableName);
}