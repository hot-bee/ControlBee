using System.Data;
using ControlBee.Constants;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class EventManager(IDatabase db) : IEventManager
{
    public void Write(
        string actorName,
        string itemPath,
        string name,
        DialogSeverity severity = DialogSeverity.Info,
        int? code = null,
        string? desc = null
    )
    {
        db.WriteEvents(actorName, name, severity.ToString(), code?.ToString(), desc);
        OnEventOccured(
            new EventMessage
            {
                EventTime = DateTime.Now, // TODO: Get from DB.
                ActorName = actorName,
                ItemPath = itemPath,
                Name = name,
                Severity = severity,
                Code = code,
                Desc = desc,
            }
        );
    }

    public IDatabase GetDatabase()
    {
        return db;
    }

    public DataTable ReadAll(string tableName)
    {
        return db.ReadAll(tableName);
    }

    public event EventHandler<EventMessage>? EventOccured;

    protected virtual void OnEventOccured(EventMessage e)
    {
        EventOccured?.Invoke(this, e);
    }
}
