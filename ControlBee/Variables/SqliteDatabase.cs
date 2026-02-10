using System.Collections.Concurrent;
using System.Data;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using ControlBeeAbstract.Exceptions;
using log4net;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace ControlBee.Variables;

public class SqliteDatabase : IDatabase, IDisposable
{
    private readonly ConcurrentDictionary<Thread, SqliteConnection> _connections = new();
    private const string DbFileName = "machine.db";
    private static readonly ILog Logger = LogManager.GetLogger("SqliteDatabase");
    private readonly ISystemConfigurations _systemConfigurations;

    public SqliteDatabase(ISystemConfigurations systemConfigurations)
    {
        _systemConfigurations = systemConfigurations;
        CreateTables();
    }

    protected static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new RespectSystemTextJsonIgnoreResolver(),
        Formatting = Formatting.Indented,
    };
    private string DbFilePath => Path.Combine(_systemConfigurations.DataFolder, DbFileName);

    public int WriteVariables(
        VariableScope scope,
        string localName,
        string actorName,
        string itemPath,
        string value
    )
    {
        var sql = """
            INSERT INTO variables (scope, local_name, actor_name, item_path, value)
            VALUES (@scope, @local_name, @actor_name, @item_path, @value)
            ON CONFLICT(local_name, actor_name, item_path) DO UPDATE SET
                value      = excluded.value,
                updated_at = datetime('now','localtime')
            RETURNING id;
            """;

        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            command.Parameters.AddWithValue("@scope", scope);
            command.Parameters.AddWithValue("@local_name", localName);
            command.Parameters.AddWithValue("@actor_name", actorName);
            command.Parameters.AddWithValue("@item_path", itemPath);
            command.Parameters.AddWithValue("@value", value);

            var id = (long)command.ExecuteScalar()!;
            return (int)id;
        }
        catch (Exception ex)
        {
            Logger.Error($"WriteVariables failed. {ex.Message}");
            throw new DatabaseError(ex.Message);
        }
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

        using var command = new SqliteCommand(sql, GetConnection());
        command.Parameters.AddWithValue("@actor_name", actorName);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@code", code ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@desc", desc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@severity", severity);

        command.ExecuteNonQuery();
    }

    public DataTable ReadAll(string tableName, QueryOptions? options = null)
    {
        var conditions = new List<string>();
        if (options?.StartDate.HasValue == true)
            conditions.Add("created_at >= @start_date");
        if (options?.EndDate.HasValue == true)
            conditions.Add("created_at < @end_date");

        var sql = $"SELECT * FROM {tableName}";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY id DESC";

        var dt = new DataTable();
        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            if (options?.StartDate.HasValue == true)
                command.Parameters.AddWithValue(
                    "@start_date",
                    options.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                );
            if (options?.EndDate.HasValue == true)
                command.Parameters.AddWithValue(
                    "@end_date",
                    options.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                );
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

        using var command = new SqliteCommand(sql, GetConnection());
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

    public Dictionary<
        (string localName, string actorName, string itemPath),
        (int id, string value)
    > ReadAllVariables(string localName)
    {
        var sql = """
            SELECT local_name, actor_name, item_path, id, value
            FROM variables
            WHERE local_name = @local_name OR local_name = ''
            """;

        var result =
            new Dictionary<
                (string localName, string actorName, string itemPath),
                (int id, string value)
            >();

        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            command.Parameters.AddWithValue("@local_name", localName);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var dbLocalName = reader.GetString(0);
                var actorName = reader.GetString(1);
                var itemPath = reader.GetString(2);
                var id = (int)reader.GetInt64(3);
                var value = reader.GetString(4);

                result[(dbLocalName, actorName, itemPath)] = (id, value);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ReadAllVariables failed. {ex.Message}");
        }

        return result;
    }

    public string[] GetLocalNames()
    {
        var sql = """
            SELECT local_name
            FROM variables
            GROUP BY local_name;
            """;
        using var command = new SqliteCommand(sql, GetConnection());
        using var reader = command.ExecuteReader();
        var list = new List<string>();
        while (reader.Read())
        {
            var localName = reader["local_name"] as string;
            if (!string.IsNullOrEmpty(localName))
                list.Add(localName);
        }

        return list.ToArray();
    }

    public void DeleteLocal(string localName)
    {
        if (string.IsNullOrEmpty(localName))
            return;
        const string sql = "DELETE FROM variables where local_name=@local_name";

        using var command = new SqliteCommand(sql, GetConnection());
        command.Parameters.AddWithValue("@local_name", localName);
        command.ExecuteNonQuery();
    }

    public void RenameLocalName(string sourceLocalName, string targetLocalName)
    {
        const string sql = """
            UPDATE variables
            SET local_name = @target,
                updated_at = datetime('now','localtime')
            WHERE local_name = @source;
            """;

        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            command.Parameters.AddWithValue("@source", sourceLocalName);
            command.Parameters.AddWithValue("@target", targetLocalName);

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error($"RenameLocalName failed. {ex.Message}");
            throw new DatabaseError(ex.Message);
        }
    }

    public void WriteVariableChange(IVariable variable, ValueChangedArgs valueChangedArgs)
    {
        if (variable.Id == null)
        {
            Logger.Error("Variable Id is null in WriteVariableChange.");
            return;
        }

        var sql =
            "INSERT INTO variable_changes (variable_id, location, old_value, new_value) "
            + "VALUES (@variable_id, @location, @old_value, @new_value)";

        using var command = new SqliteCommand(sql, GetConnection());
        try
        {
            var location = JsonConvert.SerializeObject(valueChangedArgs.Location, JsonSettings);
            var oldValue = JsonConvert.SerializeObject(valueChangedArgs.OldValue, JsonSettings);
            var newValue = JsonConvert.SerializeObject(valueChangedArgs.NewValue, JsonSettings);
            command.Parameters.AddWithValue("@variable_id", variable.Id);
            command.Parameters.AddWithValue("@location", location);
            command.Parameters.AddWithValue("@old_value", oldValue);
            command.Parameters.AddWithValue("@new_value", newValue);

            command.ExecuteNonQuery();
        }
        catch (JsonSerializationException exception)
        {
            Logger.Error($"Failed to WriteVariableChange. ({variable.Id}, {exception})");
        }
    }

    public DataTable ReadLatestVariableChanges()
    {
        var sql = """
            SELECT *
            FROM variable_changes
            WHERE id IN (
                SELECT MAX(id)
                FROM variable_changes
                GROUP BY variable_id, location
            );
            """;

        var dt = new DataTable();
        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            using var reader = command.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read variable changes. {ex}");
        }

        return dt;
    }

    public DataTable ReadVariableChanges(QueryOptions? options = null)
    {
        var conditions = new List<string>();
        if (options?.StartDate.HasValue == true)
            conditions.Add("a.created_at >= @start_date");
        if (options?.EndDate.HasValue == true)
            conditions.Add("a.created_at < @end_date");

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $"""
            SELECT a.id, b.local_name, b.scope, a.variable_id, b.actor_name, b.item_path,
            a.location, a.old_value, a.new_value, a.created_at
            FROM variable_changes a
            INNER JOIN variables b ON a.variable_id = b.id
            {whereClause}
            ORDER BY a.id DESC
            LIMIT 300
            """; // TODO: Support paging

        var dt = new DataTable();
        try
        {
            using var command = new SqliteCommand(sql, GetConnection());
            if (options?.StartDate.HasValue == true)
                command.Parameters.AddWithValue(
                    "@start_date",
                    options.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                );
            if (options?.EndDate.HasValue == true)
                command.Parameters.AddWithValue(
                    "@end_date",
                    options.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                );
            using var reader = command.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read variable changes. {ex}");
        }

        return dt;
    }

    private SqliteConnection GetConnection()
    {
        return _connections.GetOrAdd(
            Thread.CurrentThread,
            _ =>
            {
                var connection = new SqliteConnection($"Data Source={DbFilePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA busy_timeout=60000;";
                command.ExecuteNonQuery();

                return connection;
            }
        );
    }

    object IDatabase.GetConnection()
    {
        return GetConnection();
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            try
            {
                connection.Value.Close();
                connection.Value.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Dispose failed. {ex.Message}");
            }
        }

        _connections.Clear();
    }

    private void CreateTables()
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS variables(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scope INTEGER NOT NULL,
                    local_name TEXT NOT NULL,
                    actor_name TEXT NOT NULL,
                    item_path TEXT NOT NULL,
                    value BLOB,
                    updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    UNIQUE (local_name, actor_name, item_path)
                );
            CREATE TABLE IF NOT EXISTS variable_changes(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                variable_id INTEGER,
                location TEXT,
                old_value BLOB,
                new_value BLOB,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            );
            CREATE TABLE IF NOT EXISTS events(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    actor_name TEXT NOT NULL,
                    name TEXT NOT NULL,
                    code TEXT NULL,
                    desc TEXT NULL,
                    severity TEXT NOT NULL
                );
            CREATE TABLE IF NOT EXISTS users(
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id     TEXT    NOT NULL UNIQUE,
                password    TEXT    NOT NULL,
                name        TEXT    NOT NULL,
                level       INTEGER NOT NULL DEFAULT 0,
                created_at  TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at  TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                is_deleted  INTEGER NOT NULL DEFAULT 0
            );
            """;
        using var command = new SqliteCommand(sql, GetConnection());
        command.ExecuteNonQuery();
    }
}
