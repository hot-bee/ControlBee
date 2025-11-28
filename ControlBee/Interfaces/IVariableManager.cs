using System.ComponentModel;
using System.Data;

namespace ControlBee.Interfaces;

public interface IVariableManager: INotifyPropertyChanged
{
    void Add(IVariable variable);
    void Save(string? localName = null);
    void Load(string? localName = null);
    string LocalName { get; }
    string[] LocalNames { get; }
    bool Modified { get; }
    void Delete(string localName);
    DataTable ReadVariableChanges();
    void SaveTemporaryVariables();
    void DiscardChanges();
    T ReadVariable<T>(string localName, string actorName, string itemPath) where T : new();
    void WriteVariable(string localName, string actorName, string itemPath, string value);
    void WriteVariable<T>(string localName, string actorName, string itemPath, T value) where T : new();
    object ReadVariable(Type variableType, string localName, string actorName, string itemPath);
    void WriteVariable(Type variableType, string localName, string actorName, string itemPath, object value);
}
