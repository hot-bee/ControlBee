using System.IO;
using ControlBee.Interfaces;
using ControlBee.Utils;
using YamlDotNet.Serialization;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class SystemPropertiesDataSource : ISystemPropertiesDataSource
{
    private Dict? _data;
    private const string BackupDir = "backup";
    private const string PropertyFileName = "ActorProperties.yaml";

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
        using var reader = new StreamReader(PropertyFileName);
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
        if (paths.Length == 0) return;

        var parentPath = paths[..^1];
        if (!CreatePath(parentPath)) return;

        var current = parentPath.Aggregate(_data, (prev, path) => (Dict)prev![path]!);
        current![paths[^1]] = value;
    }

    public bool CreatePath(string[] paths)
    {
        if (_data is null) return false;

        var current = _data;
        foreach (var path in paths)
        {
            if (current.TryGetValue(path, out var child) && child is not Dict)
                return false;

            current = (Dict)(current[path] = child as Dict ?? new Dict());
        }

        return true;
    }

    private void BackupOriginFile()
    {
        if (!Directory.Exists(BackupDir))
            Directory.CreateDirectory(BackupDir);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var destFile = Path.Combine(BackupDir, $"{timestamp}{PropertyFileName}");

        File.Copy(PropertyFileName, destFile, overwrite: true);
    }

    public void SaveToFile()
    {
        BackupOriginFile();

        var serializer = new SerializerBuilder().Build();
        var objData = serializer.Serialize(_data)!;
        using var writer = new StreamWriter(PropertyFileName);

        writer.WriteLine(objData);
    }

    private Dict ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder().Build();
        var objData = (Dictionary<object, object>)deserializer.Deserialize(content)!;
        return DictCopy.Copy(objData);
    }
}