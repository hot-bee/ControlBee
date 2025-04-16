using ControlBee.Interfaces;

public class LogWriter(IDatabase db) : IEventWriter
{
    public void Write(
        string actorName,
        string code,
        string name,
        string desc,
        string severity
        )
    {
        db.Write(actorName, code, name, desc, severity);
    }
}
