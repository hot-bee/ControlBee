using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array3D<T> : ArrayBase, IIndex3D
    where T : new()
{
    private T[,,] _value;

    public Array3D()
        : this(0, 0, 0) { }

    public Array3D(int size1, int size2, int size3)
    {
        _value = new T[size1, size2, size3];
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
        for (var k = 0; k < Size.Item3; k++)
            _value[i, j, k] = new T();
        UpdateSubItem();
    }

    public Array3D(Array3D<T> other)
    {
        _value = (T[,,])other._value.Clone();
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
        for (var k = 0; k < Size.Item3; k++)
        {
            var otherValue = other[i, j, k];
            if (otherValue is ICloneable cloneable)
                otherValue = (T)cloneable.Clone();
            _value[i, j, k] = otherValue;
        }

        UpdateSubItem();
    }

    public T this[int x, int y, int z]
    {
        get => _value[x, y, z];
        set
        {
            var oldValue = _value[x, y, z];
            _value[x, y, z] = value;
            OnArrayElementChanged(new ValueChangedArgs([(x, y, z)], oldValue, value));
        }
    }

    public Tuple<int, int, int> Size =>
        new(_value.GetLength(0), _value.GetLength(1), _value.GetLength(2));

    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size.Item1; i++)
            for (var j = 0; j < Size.Item2; j++)
            for (var k = 0; k < Size.Item3; k++)
                yield return _value[i, j, k];
        }
    }

    public object? GetValue(int index1, int index2, int index3)
    {
        return _value[index1, index2, index3];
    }

    public override object Clone()
    {
        return new Array3D<T>(this);
    }

    public override void ReadJson(JsonDocument jsonDoc)
    {
        var size = new List<int>();
        var sizeProp = jsonDoc.RootElement.GetProperty("Size");
        foreach (var x in sizeProp.EnumerateArray())
            size.Add(x.GetInt32());
        _value = new T[size[0], size[1], size[2]];

        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;

        var idx = 0;
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
        for (var k = 0; k < size[2]; k++)
            _value[i, j, k] = values[idx++];
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
