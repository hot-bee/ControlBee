using System.Text;
using ControlBee.Interfaces;
using ControlBee.Utils;
using YamlDotNet.Serialization;

namespace ControlBee.Models;

public class ActorItemInjectionDataSource : IActorItemInjectionDataSource
{
    private object? _data = new Dictionary<object, object>();

    public object? GetValue(string actorName, string itemPath, string propertyName)
    {
        var propertyPath = string.Join('/', itemPath.Trim('/'), propertyName.Trim('/'));
        return GetValue(actorName, propertyPath);
    }

    public object? GetValue(string actorName, string propertyPath)
    {
        var access = new NestedDictionaryAccess(_data);
        try
        {
            var globalPropertyPath = string.Join('/', actorName.Trim('/'), propertyPath.Trim('/'));
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

    private object ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize(content)!;
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
}
