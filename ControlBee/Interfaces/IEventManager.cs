using System.Data;
using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IEventManager
{
    public void Write(
        string actorName,
        string name,
        DialogSeverity severity = DialogSeverity.Info,
        int? code = null,
        string? desc = null
    );

    public DataTable ReadAll(string tableName);
    event EventHandler<EventMessage>? EventOccured;
}