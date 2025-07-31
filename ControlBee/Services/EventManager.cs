using System.Data;
using ControlBee.Constants;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class EventManager(IDatabase db) : IEventManager
{
    public void Write(
        string actorName,
        string name,
        DialogSeverity severity = DialogSeverity.Info,
        int? code = null,
        string? desc = null
    )
    {
        db.WriteEvents(actorName, name, severity.ToString(), code?.ToString(), desc);
    }

    public DataTable ReadAll(string tableName)
    {
        return db.ReadAll(tableName);
    }
}