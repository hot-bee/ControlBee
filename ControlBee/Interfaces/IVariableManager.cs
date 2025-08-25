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
    void Delete(string localName);
    DataTable ReadVariableChanges();
}
