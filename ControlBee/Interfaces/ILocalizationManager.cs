using System.ComponentModel;

namespace ControlBee.Interfaces;

public interface ILocalizationManager : INotifyPropertyChanged
{
    string this[string key] { get; }
    string Translate(string key, Dictionary<string, string>? args = null);

    // Later files override earlier ones, key by key.
    void Load(params string[] jsonPaths);
    string? GetValue(string key, Dictionary<string, string>? args = null);
}
