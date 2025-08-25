using System.ComponentModel;
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
    public void Delete(string localName)
    {
        // Empty
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
}