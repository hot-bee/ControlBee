namespace ControlBee.Interfaces;

public interface IVariableManager
{
    void Add(IVariable variable);
    void Save(string? localName = null);
    void Load(string? localName = null);
}
