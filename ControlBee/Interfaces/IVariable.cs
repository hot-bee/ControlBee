using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IVariable : IActorItem, INotifyValueChanged
{
    int? Id { get; set; }
    object? ValueObject { get; }
    VariableScope Scope { get; }
    string ActorName { get; }
    public bool Dirty { get; set; }
    IUserInfo? UserInfo { get; set; }
    string ToJson();
    void FromJson(string data);
}