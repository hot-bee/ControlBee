using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Interfaces;

public interface IUserManager
{
    bool Register(string userId, string rawPassword, string name, int level = 0);
    bool Login(string userId, string userPassword);
    void Logout();
    bool Delete(int id);
    IUserInfo? CurrentUser { get; }
    event EventHandler? CurrentUserChanged;
    event EventHandler? UserListUpdated;
    List<IUserInfo> GetUserBelowCurrentLevel();
    bool UpdateUsers(IEnumerable<Dict> userUpdates);
}
