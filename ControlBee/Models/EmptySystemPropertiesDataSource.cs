using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptySystemPropertiesDataSource : ISystemPropertiesDataSource
{
    public static ISystemPropertiesDataSource Instance = new EmptySystemPropertiesDataSource();

    private EmptySystemPropertiesDataSource() { }

    public object? GetValue(string actorName, string itemPath, string propertyName)
    {
        return null;
    }

    public object? GetValue(string actorName, string propertyPath)
    {
        return null;
    }

    public void SetValue(string actorName, string propertyPath, object value)
    {
        // Empty
    }

    public void SaveToFile()
    {
        // Empty
    }

    public void ReadFromFile()
    {
        // Empty
    }

    public void ReadFromString(string content)
    {
        // Empty
    }

    public object? GetValue(string propertyPath)
    {
        return null;
    }
}
