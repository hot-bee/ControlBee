using ControlBee.Interfaces;

namespace ControlBee.Variables;

public class EmptyVariableManager : IVariableManager
{
    public void Add(IVariable variable) { }

    public void Save(string? localName = null) { }

    public void Load(string? localName = null) { }
}
