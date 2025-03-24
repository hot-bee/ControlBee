using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class PlaceholderManager
{
    private readonly Dictionary<IPlaceholder, object> _map = new();

    public void Add(IPlaceholder holder, object actualObject)
    {
        if (!_map.TryAdd(holder, actualObject))
            throw new ValueError();
    }

    public object Get(IPlaceholder holder)
    {
        return _map[holder];
    }

    public T TryGet<T>(T possiblyHolder)
    {
        if (possiblyHolder is IPlaceholder holder)
            return (T)Get(holder);

        return possiblyHolder;
    }
}
