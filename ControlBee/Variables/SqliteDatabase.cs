using System.Data;
using ControlBee.Interfaces;
using Microsoft.Data.Sqlite;

namespace ControlBee.Variables;

public class SqliteDatabase : IDatabase, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDatabase()
    {
        _connection = new SqliteConnection("Data Source=machine.db");
        _connection.Open();
        CreateTables();
    }

    public void Write(
        VariableScope scope,
        string localName,
        string actorName,
        string itemName,
        string value
    )
    {
        var sql =
            "INSERT OR REPLACE INTO variables (actor_name, item_name, scope, local_name, value) "
            + "VALUES (@actor_name, @item_name, @scope, @local_name, @value)";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@scope", scope);
        command.Parameters.AddWithValue("@local_name", localName);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@item_name", itemName);
        command.Parameters.AddWithValue("@value", value);

        command.ExecuteNonQuery();
    }

    public string? Read(string localName, string actorName, string itemName)
    {
        var sql =
            "SELECT value FROM variables "
            + "WHERE actor_name = @actor_name and item_name = @item_name and local_name = @local_name";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@local_name", localName);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@item_name", itemName);

        using var reader = command.ExecuteReader();

        var dt = new DataTable();
        dt.Load(reader);
        var numRows = dt.Rows.Count;
        if (numRows == 0)
            return null;
        if (numRows > 1)
            throw new ApplicationException(
                "Data inconsistency detected: More than two rows found for the same variable."
            );

        var value = dt.Rows[0][0].ToString();
        return value;
    }

    public void Dispose()
    {
        _connection.Close();
    }

    private void CreateTables()
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS variables(
                    scope INTEGER NOT NULL,
                    local_name TEXT NOT NULL,
                    actor_name TEXT NOT NULL,
                    item_name TEXT NOT NULL,
                    value BLOB,
                    updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    UNIQUE (local_name, actor_name, item_name)
                );
            """;
        using var command = new SqliteCommand(sql, _connection);
        command.ExecuteNonQuery();
    }
}
