using System.ComponentModel;

namespace ControlBee.Interfaces;

public interface ILocalizationManager : INotifyPropertyChanged
{
    string this[string key] { get; }
    string Translate(string key, Dictionary<string, string>? args = null);
    void Load(string jsonPath);
    string? GetValue(string key, Dictionary<string, string>? args = null);
}
