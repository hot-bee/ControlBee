using ControlBee.Interfaces;
using log4net;
using Microsoft.Data.Sqlite;

namespace ControlBee.Services;

public class UserManager : IUserManager
{
    private static readonly ILog Logger = LogManager.GetLogger("General");
    private readonly SqliteConnection _connection;
    private readonly IUserInfo _userInfo;

    public UserManager(IDatabase database, IUserInfo userInfo)
    {
        _connection = (SqliteConnection)database.GetConnection();
        _userInfo = userInfo;
    }
    
    // Written by GPT
    public bool Register(string userId, string rawPassword, string name, int level = 0)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(rawPassword))
            return false;

        try
        {
            using (var check = _connection.CreateCommand())
            {
                check.CommandText = @"SELECT 1 FROM users WHERE user_id = @user_id LIMIT 1;";
                check.Parameters.AddWithValue("@user_id", userId);
                using var reader = check.ExecuteReader();
                if (reader.Read()) return false;
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            using var beginTransaction = _connection.BeginTransaction();
            using var command = _connection.CreateCommand();
            command.Transaction = beginTransaction;
            command.CommandText = @"
                INSERT INTO users (user_id, password, name, level, created_at, updated_at)
                VALUES (@user_id, @password, @name, @level, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
            command.Parameters.AddWithValue("@user_id", userId);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@level", level);

            var rows = command.ExecuteNonQuery();
            beginTransaction.Commit();

            return rows == 1;
        }
        catch (Exception ex)
        {
            Logger.Error("Register Error", ex);
            return false;
        }
    }

    public bool Login(string userId, string userPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userPassword))
            return false;

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT id, user_id, password, name, level, created_at, updated_at
                FROM users
                WHERE user_id = @user_id
                LIMIT 1;";
            command.Parameters.AddWithValue("@user_id", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return false;

            var dbId = reader.GetInt32(0);
            var dbUserId = reader.GetString(1);
            var dbPassword = reader.GetString(2);
            var dbName = reader.GetString(3);
            var dbLevel= reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

            var ok = BCrypt.Net.BCrypt.Verify(userPassword, dbPassword);
            if (!ok) return false;

            _userInfo.UpdateFromLogin(dbId, dbUserId, dbName, dbLevel);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Login Error", ex);
            return false;
        }
    }
}