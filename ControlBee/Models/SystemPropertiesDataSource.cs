using ControlBee.Interfaces;
using ControlBee.Utils;
using YamlDotNet.Serialization;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class SystemPropertiesDataSource : ISystemPropertiesDataSource
{
    private Dict? _data;

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
        using var reader = new StreamReader("ActorProperties.yaml");
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

    private Dict ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder().Build();
        var objData = (Dictionary<object, object>)deserializer.Deserialize(content)!;
        return DictCopy.Copy(objData);
    }
}