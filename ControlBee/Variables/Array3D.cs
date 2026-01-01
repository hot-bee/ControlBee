using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array3D<T> : ArrayBase, IIndex3D, IWriteData
    where T : new()
{
    public Array3D()
        : this(0, 0, 0) { }

    public Array3D(int size1, int size2, int size3)
    {
        Values = new T[size1, size2, size3];
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
        for (var k = 0; k < Size.Item3; k++)
            Values[i, j, k] = new T();
        UpdateSubItem();
    }

    public Array3D(Array3D<T> other)
    {
        Actor = other.Actor;
        ItemPath = other.ItemPath;
        Values = (T[,,])other.Values.Clone();
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
        for (var k = 0; k < Size.Item3; k++)
        {
            var otherValue = other[i, j, k];
            if (otherValue is ICloneable cloneable)
                otherValue = (T)cloneable.Clone();
            Values[i, j, k] = otherValue;
        }

        UpdateSubItem();
    }

    public T this[int x, int y, int z]
    {
        get => Values[x, y, z];
        set
        {
            var oldValue = Values[x, y, z];
            var valueChangedArgs = new ValueChangedArgs([(x, y, z)], oldValue, value);
            OnValueChanging(valueChangedArgs);
            Values[x, y, z] = value;
            OnValueChanged(valueChangedArgs);
        }
    }

    [JsonIgnore]
    public Tuple<int, int, int> Size =>
        new(Values.GetLength(0), Values.GetLength(1), Values.GetLength(2));

    [JsonIgnore]
    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size.Item1; i++)
            for (var j = 0; j < Size.Item2; j++)
            for (var k = 0; k < Size.Item3; k++)
                yield return Values[i, j, k];
        }
    }

    public T[,,] Values { get; set; }

    public object? GetValue(int index1, int index2, int index3)
    {
        return Values[index1, index2, index3];
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        var index = ((int, int, int))args.Location[0];
        if (args.Location.Length == 1)
        {
            args.EnsureNewValueInRange();
            this[index.Item1, index.Item2, index.Item3] = (T)args.NewValue;
        }
        else
        {
            (this[index.Item1, index.Item2, index.Item3] as IWriteData)?.WriteData(
                new ItemDataWriteArgs(args)
                {
                    Location = args.Location[1..],
                    NewValue = args.NewValue,
                }
            );
        }
    }

    public T[,,] ToArray()
    {
        return (T[,,])Values.Clone();
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        throw new NotImplementedException();
    }

    public override object Clone()
    {
        return new Array3D<T>(this);
    }

    [Obsolete]
    public override void ReadJson(JsonDocument jsonDoc)
    {
        var size = new List<int>();
        var sizeProp = jsonDoc.RootElement.GetProperty("Size");
        foreach (var x in sizeProp.EnumerateArray())
            size.Add(x.GetInt32());
        Values = new T[size[0], size[1], size[2]];

        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;

        var idx = 0;
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
        for (var k = 0; k < size[2]; k++)
            Values[i, j, k] = values[idx++];
    }

    [Obsolete]
    public override void WriteJson(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        writer.WriteStartArray("Size");
        for (var i = 0; i < Values.Rank; i++)
            writer.WriteNumberValue(Values.GetLength(i));
        writer.WriteEndArray();

        var linearValue = new T[Values.Length];
        var idx = 0;
        foreach (var x in Values)
            linearValue[idx++] = x;

        writer.WritePropertyName("Values");
        writer.WriteRawValue(JsonSerializer.Serialize(linearValue));

        writer.WriteEndObject();
    }
}
