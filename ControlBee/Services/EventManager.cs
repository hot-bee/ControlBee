using System.Data;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class EventManager(IDatabase db) : IEventManager
{
    public void Write(
        string actorName,
        string name,
        string? code = null,
        string? desc = null,
        string? severity = null
    )
    {
        db.WriteEvents(actorName, name, code, desc, severity);
    }

    public DataTable ReadAll(string tableName)
    {
        return db.ReadAll(tableName);
    }
}