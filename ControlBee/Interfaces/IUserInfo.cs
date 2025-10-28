using System.ComponentModel;

namespace ControlBee.Interfaces;

public interface IUserInfo : INotifyPropertyChanged
{
    int Id { get; }
    string UserId { get; }
    string Name { get; }
    int Level { get; }

    bool IsLoggedIn { get; }
    string UserLevelName { get; }

    void UpdateFromLogin(int id, string userId, string name, int level);
}