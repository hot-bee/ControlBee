using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class PositionAxesMap : IPositionAxesMap
{
    private readonly Dictionary<string, IAxis[]> _map = new();
    private readonly Dictionary<IVariable, IAxis[]> _variableMap = new();

    public PositionAxesMap()
    {
        _map[string.Empty] = [];
    }

    public void Add(IVariable variable, IAxis[] axes)
    {
        _variableMap.Add(variable, axes);
    }

    public IAxis[] Get(string itemPath)
    {
        if (!_map.TryGetValue(itemPath, out var axis))
            throw new PlatformException(
                "PositionAxesMap must include axis information for the actor item before it can be used."
            );
        return axis;
    }

    public void UpdateMap()
    {
        foreach (var (variable, axes) in _variableMap)
            _map.Add(variable.ItemPath, axes);
        _variableMap.Clear();
    }
}
