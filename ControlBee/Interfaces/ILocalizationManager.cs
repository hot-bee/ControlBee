namespace ControlBee.Interfaces;

public interface ILocalizationManager
{
    string Translate(string key, Dictionary<string, string>? args = null);
    void Load(string jsonPath);
    string? GetValue(string key);
}
