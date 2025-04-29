using System.Data;

public interface IEventManager
{
    void Write(
        string actorName,
        string code = "",
        string name = "",
        string desc = "",
        string severity = ""
        );

    public DataTable ReadAll(string tableName);
}
