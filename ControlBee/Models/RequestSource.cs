using ControlBee.Interfaces;

namespace ControlBee.Models;

public class RequestSource(IActor actor, string requestName)
{
    public IActor Actor { get; } = actor;
    public string RequestName { get; } = requestName;

    public override bool Equals(object? obj)
    {
        return obj is RequestSource otherSource && Equals(otherSource);
    }

    protected bool Equals(RequestSource other)
    {
        return Actor.Equals(other.Actor) && RequestName == other.RequestName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Actor, RequestName);
    }
}
