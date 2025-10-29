using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Services;

public class UserInfo : IUserInfo
{
    private readonly IAuthorityLevels _authorityLevels;

    private int _id;
    private string _userId;
    private string _name;
    private int _level;

    public UserInfo(IAuthorityLevels authorityLevels, int id, string userId, string name, int level)
    {
        _authorityLevels = authorityLevels;
        _id = id;
        _userId = userId;
        _name = name;
        _level = level;
    }

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string UserId
    {
        get => _userId;
        set => SetField(ref _userId, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public int Level
    {
        get => _level;
        set
        {
            if (SetField(ref _level, value))
                OnPropertyChanged(nameof(LevelName));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public string LevelName => _authorityLevels?.GetLevelName(Level) ?? "Guest";
}
