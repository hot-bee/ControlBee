using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array1D<T> : ArrayBase
{
    private T[] _value;

    public Array1D()
        : this(0) { }

    public Array1D(int size)
    {
        CheckType(typeof(T));
        _value = new T[size];
    }

    public T this[int x]
    {
        get => _value[x];
        set
        {
            var oldValue = _value[x];
            _value[x] = value;
            OnArrayElementChanged(new ValueChangedEventArgs((x), oldValue, value));
        }
    }

    public int Size => _value.Length;

    public override void ReadJson(JsonDocument jsonDoc)
    {
        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;
        _value = values;
    }

    public override void WriteJson(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        writer.WriteStartArray("Size");
        for (var i = 0; i < _value.Rank; i++)
            writer.WriteNumberValue(_value.GetLength(i));
        writer.WriteEndArray();

        var linearValue = new T[_value.Length];
        var idx = 0;
        foreach (var x in _value)
            linearValue[idx++] = x;

        writer.WritePropertyName("Values");
        writer.WriteRawValue(JsonSerializer.Serialize(linearValue));

        writer.WriteEndObject();
    }
}
