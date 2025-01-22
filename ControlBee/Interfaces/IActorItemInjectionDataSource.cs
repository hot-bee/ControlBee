namespace ControlBee.Interfaces;

public interface IActorItemInjectionDataSource
{
    object? GetValue(string actorName, string itemPath, string propertyName);
    void ReadFromFile();
    void ReadFromString(string content);
}
