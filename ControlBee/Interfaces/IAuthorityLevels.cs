namespace ControlBee.Interfaces;

public interface IAuthorityLevels
{
    IReadOnlyDictionary<int, string> LevelMap { get; }
    string GetLevelName(int level) => LevelMap.TryGetValue(level, out var name) ? name : $"Level {level}";
}