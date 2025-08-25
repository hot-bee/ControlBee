using System.Data;
using ControlBee.Interfaces;
using log4net;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

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

    public int WriteVariables(VariableScope scope,
        string localName,
        string actorName,
        string itemPath,
        string value)
    {
        var sql = """
                  INSERT INTO variables (scope, local_name, actor_name, item_path, value)
                  VALUES (@scope, @local_name, @actor_name, @item_path, @value)
                  ON CONFLICT(local_name, actor_name, item_path) DO UPDATE SET
                      value      = excluded.value,
                      updated_at = datetime('now','localtime')
                  RETURNING id;
                  """;

        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@scope", scope);
        command.Parameters.AddWithValue("@local_name", localName);
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@item_path", itemPath);
        command.Parameters.AddWithValue("@value", value);

        var id = (long)command.ExecuteScalar()!;
        return (int)id;
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
        // TODO: Should not read all. This is a very expensive.
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

    public (int id, string value)? Read(string localName, string actorName, string itemPath)
    {
        var sql =
            "SELECT id, value FROM variables "
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

        var id = (int)(long)dt.Rows[0][0];
        var value = dt.Rows[0][1].ToString()!;
        return (id, value);
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

    public void WriteVariableChange(IVariable variable, ValueChangedArgs valueChangedArgs)
    {
        var sql =
            "INSERT INTO variable_changes (variable_id, location, old_value, new_value) "
            + "VALUES (@variable_id, @location, @old_value, @new_value)";

        using var command = new SqliteCommand(sql, _connection);
        try
        {
            var location = JsonConvert.SerializeObject(valueChangedArgs.Location);
            var oldValue = JsonConvert.SerializeObject(valueChangedArgs.OldValue);
            var newValue = JsonConvert.SerializeObject(valueChangedArgs.NewValue);
            command.Parameters.AddWithValue("@variable_id", variable.Id);
            command.Parameters.AddWithValue("@location", location);
            command.Parameters.AddWithValue("@old_value", oldValue);
            command.Parameters.AddWithValue("@new_value", newValue);

            command.ExecuteNonQuery();
        }
        catch (JsonSerializationException exception)
        {
            Logger.Error($"Failed to WriteVariableChange. {variable.Id}");
        }
    }

    public DataTable ReadVariableChanges()
    {
        var sql = """
                  SELECT a.id, b.local_name, b.scope, a.variable_id, b.actor_name, b.item_path, 
                  a.location, a.old_value, a.new_value, a.created_at
                  FROM variable_changes a
                  INNER JOIN variables b ON a.variable_id = b.id
                  ORDER BY a.id DESC
                  LIMIT 300
                  """; // TODO: Support paging

        var dt = new DataTable();
        try
        {
            using var command = new SqliteCommand(sql, _connection);
            using var reader = command.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read variable changes. {ex}");
        }

        return dt;
    }

    public void Dispose()
    {
        _connection.Close();
    }

    private void CreateTables()
    {
        var sql = """
                  CREATE TABLE IF NOT EXISTS variables(
                          id INTEGER PRIMARY KEY,
                          scope INTEGER NOT NULL,
                          local_name TEXT NOT NULL,
                          actor_name TEXT NOT NULL,
                          item_path TEXT NOT NULL,
                          value BLOB,
                          updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                          UNIQUE (local_name, actor_name, item_path)
                      );
                  CREATE TABLE IF NOT EXISTS variable_changes(
                      id INTEGER PRIMARY KEY,
                      variable_id INTEGER,
                      location TEXT,
                      old_value BLOB,
                      new_value BLOB,
                      created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
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