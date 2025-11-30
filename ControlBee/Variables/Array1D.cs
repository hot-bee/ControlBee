using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array1D<T> : ArrayBase, IIndex1D, IDisposable, IWriteData
    where T : new()
{
    private static readonly ILog Logger = LogManager.GetLogger("Array1D");

    public Array1D()
        : this(0)
    {
    }

    public Array1D(int size)
    {
        Values = new T[size];
        for (var i = 0; i < Size; i++)
        {
            Values[i] = new T();
            Subscribe(i);
        }

        UpdateSubItem();
    }

    public Array1D(Array1D<T> other)
    {
        Actor = other.Actor;
        ItemPath = other.ItemPath;
        Values = (T[])other.Values.Clone();
        for (var i = 0; i < Size; i++)
        {
            var otherValue = other[i];
            if (otherValue is ICloneable cloneable)
                otherValue = (T)cloneable.Clone();
            Values[i] = otherValue;
            Subscribe(i);
        }

        UpdateSubItem();
    }

    public Array1D(T[] values)
    {
        Values = values;
        UpdateSubItem();
    }

    public T[] Values { get; set; }

    public T this[int x]
    {
        get => Values[x];
        set
        {
            var oldValue = Values[x];
            OnValueChanging(new ValueChangedArgs([x], oldValue, value));
            Unsubscribe(x);
            Values[x] = value;
            Subscribe(x);
            OnValueChanged(new ValueChangedArgs([x], oldValue, value));
        }
    }

    [JsonIgnore]
    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size; i++)
                yield return Values[i];
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < Size; i++)
            Unsubscribe(i);
    }

    [JsonIgnore]
    public int Size => Values.Length;

    public object? GetValue(int index)
    {
        return Values[index];
    }

    public void SetValue(int index, object value)
    {
        this[index] = (T)value;
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        var index = (int)args.Location[0];
        if (args.Location.Length == 1)
        {
            args.EnsureNewValueInRange();
            this[index] = (T)args.NewValue;
        }
        else
        {
            (this[index] as IWriteData)?.WriteData(
                new ItemDataWriteArgs(args)
                {
                    Location = args.Location[1..],
                    NewValue = args.NewValue
                }
            );
        }
    }

    public T[] ToArray()
    {
        return (T[])Values.Clone();
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        for (var i = 0; i < Size; i++)
        {
            if (Values[i] is IActorItemSub actorItemSub)
                actorItemSub.OnDeserialized();
            Subscribe(i);
        }
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        if (message is VariableActorItemMessage variableActorItemMessage)
        {
            var index = (int)variableActorItemMessage.Location[0];
            if (Values[index] is IActorItemSub actorItemSub)
            {
                var partialLocation = variableActorItemMessage.Location[1..];
                var partialMessage = new VariableActorItemMessage(message.Sender, message.ItemPath, partialLocation,
                    message.Name, message.Payload);
                actorItemSub.ProcessMessage(partialMessage);
                return true;
            }
        }

        return false;
    }

    public override object Clone()
    {
        return new Array1D<T>(this);
    }

    private void Subscribe(int index)
    {
        if (Values[index] is INotifyValueChanged notify)
        {
            notify.ValueChanging += NotifyOnValueChanging;
            notify.ValueChanged += NotifyOnValueChanged;
        }
    }

    private void Unsubscribe(int index)
    {
        if (Values[index] is INotifyValueChanged notify)
        {
            notify.ValueChanging -= NotifyOnValueChanging;
            notify.ValueChanged -= NotifyOnValueChanged;
        }
    }

    private void NotifyOnValueChanging(object? sender, ValueChangedArgs e)
    {
        var index = Array.IndexOf(Values, sender);
        if (index == -1)
        {
            Logger.Warn($"Couldn't find index of the changed value. ({sender})");
            return;
        }

        OnValueChanging(
            new ValueChangedArgs(
                ((object[]) [index]).Concat(e.Location).ToArray(),
                e.OldValue,
                e.NewValue
            )
        );
    }

    private void NotifyOnValueChanged(object? sender, ValueChangedArgs e)
    {
        var index = Array.IndexOf(Values, sender);
        if (index == -1)
        {
            Logger.Warn($"Couldn't find index of the changed value. ({sender})");
            return;
        }

        OnValueChanged(
            new ValueChangedArgs(
                ((object[]) [index]).Concat(e.Location).ToArray(),
                e.OldValue,
                e.NewValue
            )
        );
    }

    [Obsolete]
    public override void ReadJson(JsonDocument jsonDoc)
    {
        var valuesProp = jsonDoc.RootElement.GetProperty("Values");
        var values = valuesProp.Deserialize<T[]>()!;
        Values = values;
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