using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Interfaces;

public interface IUserManager
{
    bool Register(string userId, string rawPassword, string name, int level = 0);
    bool Login(string userId, string userPassword);
    IUserInfo? CurrentUser { get; }
    event EventHandler? CurrentUserChanged;
    List<IUserInfo> GetUserBelowCurrentLevel();
    bool UpdateUsers(IEnumerable<Dict> userUpdates);
}