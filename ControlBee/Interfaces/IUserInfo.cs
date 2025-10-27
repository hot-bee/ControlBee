namespace ControlBee.Interfaces;

public interface IUserInfo
{
    int Id { get; }
    string UserId { get; }
    string Password { get; }
    string Name { get; }
    int Level { get; }

    bool ValidateUser(string userId, string userPassword);
}