using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class UserInfo : IUserInfo
{
    private int _id;
    private string _userId = string.Empty;
    private string _name = string.Empty;
    private int _level;

    public int Id
    {
        get => _id;
        private set
        {
            if (Set(ref _id, value))
                OnPropertyChanged(nameof(IsLoggedIn));
        }
    }

    public string UserId
    {
        get => _userId;
        private set => Set(ref _userId, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public int Level
    {
        get => _level;
        private set
        {
            if (Set(ref _level, value))
                OnPropertyChanged(nameof(UserLevelName));
        }
    }

    public bool IsLoggedIn => Id > 0;

    public string UserLevelName => Level switch
    {
        0 => "Guest",
        1 => "Operator",
        3 => "Maintenance",
        5 => "Manager",
        7 => "Manufacturer Engineer",
        9 => "Software Engineer",
        _ => $"Level {Level}"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void UpdateFromLogin(int id, string userId, string name, int level)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Level = level;
    }
}
