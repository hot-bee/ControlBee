namespace ControlBee.Interfaces;

public interface IAuthorityLevels
{
    IReadOnlyDictionary<int, string> LevelMap { get; }
    string GetLevelName(int level);
}
