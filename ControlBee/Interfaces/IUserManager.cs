namespace ControlBee.Interfaces;

public interface IUserManager
{
    bool Register(string userId, string rawPassword, string name, int level = 0);
    bool Login(string userId, string userPassword);
    IUserInfo? CurrentUser { get; }
    event EventHandler? CurrentUserChanged;
    string GetCurrentUserLevelName { get; }
}
