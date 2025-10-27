using ControlBee.Interfaces;
using Microsoft.Data.Sqlite;

namespace ControlBee.Services;

public class UserManager : IUserInfo
{
    private readonly string _dbPath;

    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Level { get; private set; }

    public UserManager()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "machine.db");
    }

    // Written by GPT.
    public bool ValidateUser(string userId, string userPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userPassword))
            return false;

        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath};Cache=Shared");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, user_id, password, name, level, created_at, updated_at
                FROM users
                WHERE user_id = @user_id
                LIMIT 1;";
            command.Parameters.AddWithValue("@user_id", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read()) 
                return false;

            var dbPassword = reader.GetString(2);

            var ok = string.Equals(dbPassword, userPassword, StringComparison.Ordinal);
            if (!ok) return false;

            Id = reader.GetInt32(0);
            UserId = reader.GetString(1);
            Password = dbPassword;
            Name = reader.GetString(3);
            Level = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
