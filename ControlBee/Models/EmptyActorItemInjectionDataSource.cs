using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyActorItemInjectionDataSource : IActorItemInjectionDataSource
{
    public static IActorItemInjectionDataSource Instance = new EmptyActorItemInjectionDataSource();

    private EmptyActorItemInjectionDataSource() { }

    public object? GetValue(string actorName, string itemPath, string propertyName)
    {
        return null;
    }

    public void ReadFromFile()
    {
        // Empty
    }

    public void ReadFromString(string content)
    {
        // Empty
    }
}
