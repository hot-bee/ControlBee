using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array2D<T> : ArrayBase, IIndex2D, IWriteData
    where T : new()
{
    public T[,] Values { get; set; }

    public Array2D()
        : this(0, 0) { }

    public Array2D(int size1, int size2)
    {
        Values = new T[size1, size2];
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
            Values[i, j] = new T();
        UpdateSubItem();
    }

    public T[,] ToArray()
    {
        return (T[,])Values.Clone();
    }

    public Array2D(Array2D<T> other)
    {
        Actor = other.Actor;
        ItemPath = other.ItemPath;
        Values = (T[,])other.Values.Clone();
        for (var i = 0; i < Size.Item1; i++)
        for (var j = 0; j < Size.Item2; j++)
        {
            var otherValue = other[i, j];
            if (otherValue is ICloneable cloneable)
                otherValue = (T)cloneable.Clone();
            Values[i, j] = otherValue;
        }
        UpdateSubItem();
    }

    public T this[int x, int y]
    {
        get => Values[x, y];
        set
        {
            var oldValue = Values[x, y];
            var valueChangedArgs = new ValueChangedArgs([(x, y)], oldValue, value);
            OnValueChanging(valueChangedArgs);
            Values[x, y] = value;
            OnValueChanged(valueChangedArgs);
        }
    }

    [JsonIgnore]
    public Tuple<int, int> Size => new(Values.GetLength(0), Values.GetLength(1));

    [Obsolete]
    public override void ReadJson(JsonDocument jsonDoc)
    {
        var size = new List<int>();
        var sizeProp = jsonDoc.RootElement.GetProperty("Size");
        foreach (var x in sizeProp.EnumerateArray())
            size.Add(x.GetInt32());
        Values = new T[size[0], size[1]];

        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;

        var idx = 0;
        for (var i = 0; i < size[0]; i++)
        for (var j = 0; j < size[1]; j++)
            Values[i, j] = values[idx++];
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

    [JsonIgnore]
    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size.Item1; i++)
            for (var j = 0; j < Size.Item2; j++)
                yield return Values[i, j];
        }
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        throw new NotImplementedException();
    }

    public override object Clone()
    {
        return new Array2D<T>(this);
    }

    public object? GetValue(int index1, int index2)
    {
        return Values[index1, index2];
    }

    public void SetValue((int, int) index, object value)
    {
        SetValue(index.Item1, index.Item2, value);
    }

    public void SetValue(int index1, int index2, object value)
    {
        this[index1, index2] = (T)value;
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        var index = ((int, int))args.Location[0];
        if (args.Location.Length == 1)
        {
            args.EnsureNewValueInRange();
            this[index.Item1, index.Item2] = (T)args.NewValue;
        }
        else
        {
            (this[index.Item1, index.Item2] as IWriteData)?.WriteData(
                new ItemDataWriteArgs(args)
                {
                    Location = args.Location[1..],
                    NewValue = args.NewValue
                }
            );
        }
    }
}
