namespace ControlBee.Interfaces;

public interface IActorItemInjectionDataSource
{
    object? GetValue(string actorName, string itemPath, string propertyName);
    object? GetValue(string actorName, string propertyPath);
    void ReadFromFile();
    void ReadFromString(string content);
}
