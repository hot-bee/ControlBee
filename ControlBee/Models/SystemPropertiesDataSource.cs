using ControlBee.Interfaces;
using ControlBee.Utils;
using YamlDotNet.Serialization;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class SystemPropertiesDataSource(ISystemConfigurations systemConfigurations)
    : ISystemPropertiesDataSource
{
    private const string BackupDirName = "Backup";
    private const string PropertyFileName = "ActorProperties.yaml";
    private Dict? _data;
    private string BackupDir => Path.Combine(systemConfigurations.DataFolder, BackupDirName);
    private string PropertyFilePath =>
        Path.Combine(systemConfigurations.DataFolder, PropertyFileName);

    public object? GetValue(string actorName, string itemPath, string propertyName)
    {
        var propertyPath = string.Join('/', itemPath.Trim('/'), propertyName.Trim('/'));
        return GetValue(actorName, propertyPath);
    }

    public object? GetValue(string actorName, string propertyPath)
    {
        var globalPropertyPath = string.Join('/', actorName.Trim('/'), propertyPath.Trim('/'));
        return GetValue(globalPropertyPath);
    }

    public void ReadFromFile()
    {
        if (!File.Exists(PropertyFilePath))
            File.WriteAllText(PropertyFilePath, "Empty: true");
        using var reader = new StreamReader(PropertyFilePath);
        _data = ParseYaml(reader.ReadToEnd());
    }

    public void ReadFromString(string content)
    {
        _data = ParseYaml(content);
    }

    public object? GetValue(string propertyPath)
    {
        var access = DictPath.Start(_data);
        try
        {
            var globalPropertyPath = string.Join('/', propertyPath.Trim('/'));
            foreach (var propName in globalPropertyPath.Split("/"))
                access = access[propName];

            return access.Value;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (NullReferenceException)
        {
            return null;
        }
    }

    public void SetValue(string actorName, string propertyPath, object value)
    {
        var fullPath = string.Join('/', actorName.Trim('/'), propertyPath.Trim('/'));
        var paths = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0)
            return;

        var parentPath = paths[..^1];
        if (!CreatePath(parentPath))
            return;

        var current = parentPath.Aggregate(_data, (prev, path) => (Dict)prev![path]!);
        current![paths[^1]] = value;
    }

    public void SaveToFile()
    {
        BackupPropertyFile();

        var serializer = new SerializerBuilder().Build();
        var objData = serializer.Serialize(_data)!;
        using var writer = new StreamWriter(PropertyFilePath);

        writer.WriteLine(objData);
    }

    public bool CreatePath(string[] paths)
    {
        if (_data is null)
            return false;

        var current = _data;
        foreach (var path in paths)
        {
            if (current.TryGetValue(path, out var child) && child is not Dict)
                return false;

            current = (Dict)(current[path] = child as Dict ?? new Dict());
        }

        return true;
    }

    private void BackupPropertyFile()
    {
        Directory.CreateDirectory(BackupDir);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var destFile = Path.Combine(BackupDir, $"{timestamp}{PropertyFileName}");

        File.Copy(PropertyFileName, destFile, true);
    }

    private Dict ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder().Build();
        var objData = (Dictionary<object, object>)deserializer.Deserialize(content)!;
        return DictCopy.Copy(objData);
    }
}
