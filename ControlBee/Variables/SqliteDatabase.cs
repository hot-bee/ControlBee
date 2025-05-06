using System.Data;
using System.Reflection.Metadata.Ecma335;
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

    public void WriteVariables(
        VariableScope scope,
        string localName,
        string actorName,
        string itemPath,
        string value
    )
    {
        var sql =
            "INSERT OR REPLACE INTO variables (actor_name, item_path, scope, local_name, value) "
            + "VALUES (@actor_name, @item_path, @scope, @local_name, @value)";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@scope", scope);
        command.Parameters.AddWithValue("@local_name", localName);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@item_path", itemPath);
        command.Parameters.AddWithValue("@value", value);

        command.ExecuteNonQuery();
    }

    public void WriteEvents(
        string actorName,
        string name,
        string? code = null,
        string? desc = null,
        string? severity = null
    )
    {
        var sql =
            "INSERT OR REPLACE INTO events (actor_name, name, code, desc, severity) "
            + "VALUES (@actor_name, @name, @code, @desc, @severity)";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@code", code ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@desc", desc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@severity", severity ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    public DataTable ReadAll(string tableName)
    {
        var sql = $"SELECT * FROM {tableName}";

        var dt = new DataTable();
        try
        {
            using var command = new SqliteCommand(sql, _connection);
            using var reader = command.ExecuteReader();
            dt.Load(reader);
        }
        catch
        {
            // need to notify
        }

        return dt;
    }

    public string? Read(string localName, string actorName, string itemPath)
    {
        var sql =
            "SELECT value FROM variables "
            + "WHERE actor_name = @actor_name and item_path = @item_path and local_name = @local_name";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@local_name", localName);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@item_path", itemPath);

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
                    item_path TEXT NOT NULL,
                    value BLOB,
                    updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    UNIQUE (local_name, actor_name, item_path)
                );
            CREATE TABLE IF NOT EXISTS events(
                    id INTEGER PRIMARY KEY,
                    updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    actor_name TEXT NOT NULL,
                    name TEXT NOT NULL,
                    code TEXT NULL,
                    desc TEXT NULL,
                    severity TEXT NULL
                );
            """;
        using var command = new SqliteCommand(sql, _connection);
        command.ExecuteNonQuery();
    }
}
