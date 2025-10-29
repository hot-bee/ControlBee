using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IUserManager
{
    bool Register(string userId, string rawPassword, string name, int level = 0);
    bool Login(string userId, string userPassword);
    IUserInfo? CurrentUser { get; }
    event EventHandler? CurrentUserChanged;
    List<UserListItem> GetUserBelowCurrentLevel();
    UserUpdateResult UpdateUsersDetailed(IEnumerable<UserUpdate> userUpdates);
}

public readonly record struct UserListItem(int Id, string UserId, string Name, int Level);
public readonly record struct UserUpdate(int Id, string Name, string? RawPassword, int Level);
public sealed record UserUpdateResult(int UpdatedCount, IReadOnlyList<SkippedUserUpdate> Skipped);
public sealed record SkippedUserUpdate(int UserId, UserUpdateSkipReason Reason);