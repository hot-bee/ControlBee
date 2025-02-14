using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyActorRegistry : IActorRegistry
{
    public static EmptyActorRegistry Instance = new();

    private EmptyActorRegistry() { }

    public void Add(IActor actor)
    {
        throw new UnimplementedByDesignError();
    }

    public IActor? Get(string actorName)
    {
        throw new UnimplementedByDesignError();
    }

    public string[] GetActorNames()
    {
        throw new UnimplementedByDesignError();
    }

    public IActor[] GetActors()
    {
        throw new UnimplementedByDesignError();
    }

    public (string name, string Title)[] GetActorNameTitlePairs()
    {
        throw new UnimplementedByDesignError();
    }
}
