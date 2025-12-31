using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

public class EmptyVariableManager : IVariableManager
{
    public static EmptyVariableManager Instance = new();

    private EmptyVariableManager()
    {
    }

    public void Add(IVariable variable)
    {
    }

    public void Save(string? localName = null)
    {
    }

    public void Load(string? localName = null)
    {
    }

    public string LocalName { get; } = "";
    public string[] LocalNames { get; } = [];
    public bool Modified { get; }

    public void Delete(string localName)
    {
        // Empty
    }

    public DataTable ReadVariableChanges()
    {
        throw new NotImplementedException();
    }

    public void SaveTemporaryVariables()
    {
        throw new NotImplementedException();
    }

    public void DiscardChanges()
    {
        throw new NotImplementedException();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public T ReadVariable<T>(string localName, string actorName, string itemPath)
        where T : new()
    {
        throw new NotImplementedException();
    }

    public void WriteVariable(string localName, string actorName, string itemPath, string value)
    {
        // Empty
    }

    public void WriteVariable<T>(string localName, string actorName, string itemPath, T value) where T : new()
    {
        // Empty
    }

    public object ReadVariable(Type variableType, string localName, string actorName, string itemPath)
    {
        throw new NotImplementedException();
    }

    public void WriteVariable(Type variableType, string localName, string actorName, string itemPath, object value)
    {
        throw new NotImplementedException();
    }

    public void Reload()
    {
        throw new NotImplementedException();
    }

    public void RenameLocalName(string sourceLocalName, string targetLocalName)
    {
        // Empty
    }

    public event EventHandler<string>? LoadCompleted;
}