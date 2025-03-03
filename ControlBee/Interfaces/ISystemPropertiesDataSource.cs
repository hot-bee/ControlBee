namespace ControlBee.Interfaces;

public interface ISystemPropertiesDataSource
{
    object? GetValue(string actorName, string itemPath, string propertyName);
    object? GetValue(string actorName, string propertyPath);
    void ReadFromFile();
    void ReadFromString(string content);
    object? GetValue(string propertyPath);
}
