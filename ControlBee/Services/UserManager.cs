using ControlBee.Constants;
using ControlBee.Interfaces;
using log4net;
using Microsoft.Data.Sqlite;

namespace ControlBee.Services;

public class UserManager : IUserManager
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(UserManager));

    private readonly SqliteConnection _connection;
    private readonly IAuthorityLevels _authorityLevels;

    private IUserInfo? _currentUser;
    public IUserInfo? CurrentUser
    {
        get => _currentUser;
        set
        {   
            if (ReferenceEquals(_currentUser, value)) return;
            _currentUser = value;
            CurrentUserChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentUserChanged;

    public UserManager(IDatabase database, IAuthorityLevels authorityLevels)
    {
        _connection = (SqliteConnection)database.GetConnection();
        _authorityLevels = authorityLevels;
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
        try
        {
            var user = GetLoginUser(userId, userPassword);
            if (user is null)
                return false;

            CurrentUser = user;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Login Error", ex);
            return false;
        }
    }

    private IUserInfo? GetLoginUser(string userId, string userPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userPassword))
            return null;

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                    SELECT id, user_id, password, name, level
                    FROM users
                    WHERE user_id = @user_id
                    LIMIT 1;";
            command.Parameters.AddWithValue("@user_id", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            var dbId = reader.GetInt32(0);
            var dbUserId = reader.GetString(1);
            var dbPasswordHash = reader.GetString(2);
            var dbName = reader.GetString(3);
            var dbLevel = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

            if (!BCrypt.Net.BCrypt.Verify(userPassword, dbPasswordHash))
                return null;

            return new UserInfo(_authorityLevels, dbId, dbUserId, dbName, dbLevel);
        }
        catch (Exception ex)
        {
            Logger.Error("GetLoginUser Error", ex);
            return null;
        }
    }

    public List<UserListItem> GetUserBelowCurrentLevel()
    {
        var list = new List<UserListItem>();
        var current = CurrentUser;
        if (current is null) return list;

        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_id, name, level
            FROM users
            WHERE level < @myLevel OR id = @myId
            ORDER BY id DESC;
        """;
        command.Parameters.AddWithValue("@myLevel", current.Level);
        command.Parameters.AddWithValue("@myId", current.Id);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UserListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)
            ));
        }

        return list;
    }

    public UserUpdateResult UpdateUsersDetailed(IEnumerable<UserUpdate> updates)
    {
        var skippedList = new List<SkippedUserUpdate>();
        int updatedCount = 0;
        var currentUser = CurrentUser;

        if (currentUser is null)
            return new UserUpdateResult(0, [new SkippedUserUpdate(0, UserUpdateSkipReason.TargetNotFound)]);

        try
        {
            using var transaction = _connection.BeginTransaction();

            using var selectLevel = _connection.CreateCommand();
            selectLevel.Transaction = transaction;
            selectLevel.CommandText = "SELECT level FROM users WHERE id=@id;";
            var parameterTargetUserId = selectLevel.Parameters.Add("@id", SqliteType.Integer);

            using var commandWithoutPassword = _connection.CreateCommand();
            commandWithoutPassword.Transaction = transaction;
            commandWithoutPassword.CommandText = """
                UPDATE users
                SET name=@name, level=@level, updated_at=datetime('now','localtime')
                WHERE id=@id;
            """;
            var nameWithoutPassword = commandWithoutPassword.Parameters.Add("@name", SqliteType.Text);
            var levelWithoutPassword = commandWithoutPassword.Parameters.Add("@level", SqliteType.Integer);
            var idWithoutPassword = commandWithoutPassword.Parameters.Add("@id", SqliteType.Integer);

            using var commandWithPassword = _connection.CreateCommand();
            commandWithPassword.Transaction = transaction;
            commandWithPassword.CommandText = """
                UPDATE users
                SET name=@name, password=@password, level=@level, updated_at=datetime('now','localtime')
                WHERE id=@id;
            """;
            var nameWithPassword = commandWithPassword.Parameters.Add("@name", SqliteType.Text);
            var password = commandWithPassword.Parameters.Add("@password", SqliteType.Text);
            var levelWithPassword = commandWithPassword.Parameters.Add("@level", SqliteType.Integer);
            var idWithPassword = commandWithPassword.Parameters.Add("@id", SqliteType.Integer);

            foreach (var update in updates)
            {
                bool isSelf = update.Id == currentUser.Id;
                parameterTargetUserId.Value = update.Id;
                var targetUserCurrentLevel = selectLevel.ExecuteScalar();

                if (targetUserCurrentLevel is null)
                {
                    skippedList.Add(new SkippedUserUpdate(update.Id, UserUpdateSkipReason.TargetNotFound));
                    continue;
                }

                int targetLevel = Convert.ToInt32(targetUserCurrentLevel);

                if (isSelf)
                {
                    if (update.Level != currentUser.Level)
                    {
                        skippedList.Add(new SkippedUserUpdate(update.Id, UserUpdateSkipReason.SelfLevelChangeNotAllowed));
                        continue;
                    }
                }
                else
                {
                    if (targetLevel >= currentUser.Level)
                    {
                        skippedList.Add(new SkippedUserUpdate(update.Id, UserUpdateSkipReason.CannotEditPeerOrHigher));
                        continue;
                    }

                    if (update.Level >= currentUser.Level)
                    {
                        skippedList.Add(new SkippedUserUpdate(update.Id, UserUpdateSkipReason.LevelMustBeLowerThanCurrentUser));
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(update.RawPassword))
                {
                    nameWithoutPassword.Value = update.Name;
                    levelWithoutPassword.Value = update.Level;
                    idWithoutPassword.Value = update.Id;
                    updatedCount += commandWithoutPassword.ExecuteNonQuery();
                }
                else
                {
                    nameWithPassword.Value = update.Name;
                    password.Value = BCrypt.Net.BCrypt.HashPassword(update.RawPassword);
                    levelWithPassword.Value = update.Level;
                    idWithPassword.Value = update.Id;
                    updatedCount += commandWithPassword.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Logger.Error("UpdateUsersDetailed Error", ex);
        }

        return new UserUpdateResult(updatedCount, skippedList);
    }
}