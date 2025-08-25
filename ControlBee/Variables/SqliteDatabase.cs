using System.Data;
using ControlBee.Interfaces;
using log4net;
using Microsoft.Data.Sqlite;

namespace ControlBee.Variables;

public class SqliteDatabase : IDatabase, IDisposable
{
    private const string DbFileName = "machine.db";
    private static readonly ILog Logger = LogManager.GetLogger("SqliteDatabase");
    private readonly SqliteConnection _connection;
    private readonly ISystemConfigurations _systemConfigurations;

    public SqliteDatabase(ISystemConfigurations systemConfigurations)
    {
        _systemConfigurations = systemConfigurations;
        _connection = new SqliteConnection($"Data Source={DbFilePath}");
        _connection.Open();
        CreateTables();
    }

    private string DbFilePath => Path.Combine(_systemConfigurations.DataFolder, DbFileName);

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
        string severity,
        string? code = null,
        string? desc = null
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
        command.Parameters.AddWithValue("@severity", severity);

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
        catch (Exception ex)
        {
            Logger.Error($"ReadAll failed." + $"TableName: {tableName}" + $"Message: {ex.Message}");
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

    public string[] GetLocalNames()
    {
        var sql = """
                  SELECT local_name
                  FROM variables
                  GROUP BY local_name;
                  """;
        using var command = new SqliteCommand(sql, _connection);
        using var reader = command.ExecuteReader();
        var list = new List<string>();
        while (reader.Read())
        {
            var localName = reader["local_name"] as string;
            if (!string.IsNullOrEmpty(localName)) list.Add(localName);
        }

        return list.ToArray();
    }

    public void DeleteLocal(string localName)
    {
        if (string.IsNullOrEmpty(localName)) return;
        const string sql = "DELETE FROM variables where local_name=@local_name";

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@local_name", localName);
        command.ExecuteNonQuery();
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
                          created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                          actor_name TEXT NOT NULL,
                          name TEXT NOT NULL,
                          code TEXT NULL,
                          desc TEXT NULL,
                          severity TEXT NOT NULL
                      );
                  """;
        using var command = new SqliteCommand(sql, _connection);
        command.ExecuteNonQuery();
    }
}