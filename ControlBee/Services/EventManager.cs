using System.Data;
using ControlBee.Interfaces;

public class EventManager(IDatabase db) : IEventManager
{
    public void Write(
        string actorName,
        string code = "",
        string name = "",
        string desc = "",
        string severity = ""
        )
    {
        db.WriteEvents(actorName, code, name, desc, severity);
    }

    public DataTable ReadAll(string tableName)
    {
        return db.ReadAll(tableName);
    }
}
