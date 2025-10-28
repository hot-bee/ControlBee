using System.ComponentModel;

namespace ControlBee.Interfaces;

public interface IUserInfo : INotifyPropertyChanged
{
    int Id { get; }
    string UserId { get; }
    string Name { get; }
    int Level { get; }
}