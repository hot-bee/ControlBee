namespace ControlBee.Interfaces;

public interface IPositionAxesMap
{
    void Add(IVariable variable, IAxis[] axes);
    IAxis[] Get(string itemPath);
    void UpdateMap();
}
