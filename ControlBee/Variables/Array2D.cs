using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array2D<T> : ArrayBase
    where T : new()
{
    private T[,] _value;

    public Array2D()
        : this(0, 0) { }

    public Array2D(int size1, int size2)
    {
        _value = new T[size1, size2];
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
            _value[i, j] = new T();
        UpdateSubItem();
    }

    public T this[int x, int y]
    {
        get => _value[x, y];
        set
        {
            var oldValue = _value[x, y];
            _value[x, y] = value;
            OnArrayElementChanged(new ValueChangedEventArgs((x, y), oldValue, value));
        }
    }

    public Tuple<int, int> Size => new(_value.GetLength(0), _value.GetLength(1));

    public override void ReadJson(JsonDocument jsonDoc)
    {
        var size = new List<int>();
        var sizeProp = jsonDoc.RootElement.GetProperty("Size");
        foreach (var x in sizeProp.EnumerateArray())
            size.Add(x.GetInt32());
        _value = new T[size[0], size[1]];

        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;

        var idx = 0;
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
            _value[i, j] = values[idx++];
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

    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size.Item1; i++)
            for (var j = 0; j < Size.Item2; j++)
                yield return _value[i, j];
        }
    }
}
