using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControlBee.Variables;

public class ArrayBaseConverter : JsonConverter<ArrayBase>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(ArrayBase).IsAssignableFrom(typeToConvert);
    }

    public override ArrayBase Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument jsonDoc = JsonDocument.ParseValue(ref reader);
        var array = (ArrayBase)Activator.CreateInstance(typeToConvert)!;
        array.ReadJson(jsonDoc);
        return array;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    )
    {
        value.WriteJson(writer, value, options);
    }
}
