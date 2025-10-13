using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array1D<T> : ArrayBase, IIndex1D, IDisposable, IWriteData
    where T : new()
{
    private static readonly ILog Logger = LogManager.GetLogger("Array1D");
    private T[] _value;

    public Array1D()
        : this(0) { }

    public Array1D(int size)
    {
        _value = new T[size];
        for (var i = 0; i < Size; i++)
        {
            _value[i] = new T();
            Subscribe(i);
        }
        UpdateSubItem();
    }

    public Array1D(Array1D<T> other)
    {
        Actor = other.Actor;
        ItemPath = other.ItemPath;
        _value = (T[])other._value.Clone();
        for (var i = 0; i < Size; i++)
        {
            var otherValue = other[i];
            if (otherValue is ICloneable cloneable)
                otherValue = (T)cloneable.Clone();
            _value[i] = otherValue;
            Subscribe(i);
        }
        UpdateSubItem();
    }

    public Array1D(T[] value)
    {
        _value = value;
        UpdateSubItem();
    }

    public T this[int x]
    {
        get => _value[x];
        set
        {
            var oldValue = _value[x];
            OnValueChanging(new ValueChangedArgs([x], oldValue, value));
            Unsubscribe(x);
            _value[x] = value;
            Subscribe(x);
            OnValueChanged(new ValueChangedArgs([x], oldValue, value));
        }
    }

    public int Size => _value.Length;

    public override IEnumerable<object?> Items
    {
        get
        {
            for (var i = 0; i < Size; i++)
                yield return _value[i];
        }
    }

    public object? GetValue(int index)
    {
        return _value[index];
    }

    public void Dispose()
    {
        for (var i = 0; i < Size; i++)
            Unsubscribe(i);
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        var index = (int)args.Location[0];
        if (args.Location.Length == 1)
            this[index] = (T)args.NewValue;
        else
            (this[index] as IWriteData)?.WriteData(
                new ItemDataWriteArgs(args.Location[1..], args.NewValue)
            );
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        for (var i = 0; i < Size; i++)
        {
            if (_value[i] is IActorItemSub actorItemSub)
                actorItemSub.OnDeserialized();
            Subscribe(i);
        }
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        if (message is VariableActorItemMessage variableActorItemMessage)
        {
            var index = (int)variableActorItemMessage.Location[0];
            if (_value[index] is IActorItemSub actorItemSub)
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
        if (_value[index] is INotifyValueChanged notify)
        {
            notify.ValueChanging += NotifyOnValueChanging;
            notify.ValueChanged += NotifyOnValueChanged;
        }
    }

    private void Unsubscribe(int index)
    {
        if (_value[index] is INotifyValueChanged notify)
        {
            notify.ValueChanging -= NotifyOnValueChanging;
            notify.ValueChanged -= NotifyOnValueChanged;
        }
    }
    private void NotifyOnValueChanging(object? sender, ValueChangedArgs e)
    {
        var index = Array.IndexOf(_value, sender);
        if (index == -1)
        {
            Logger.Warn($"Couldn't find index of the changed value. ({sender})");
            return;
        }

        OnValueChanging(
            new ValueChangedArgs(
                ((object[])[index]).Concat(e.Location).ToArray(),
                e.OldValue,
                e.NewValue
            )
        );
    }
    private void NotifyOnValueChanged(object? sender, ValueChangedArgs e)
    {
        var index = Array.IndexOf(_value, sender);
        if (index == -1)
        {
            Logger.Warn($"Couldn't find index of the changed value. ({sender})");
            return;
        }

        OnValueChanged(
            new ValueChangedArgs(
                ((object[])[index]).Concat(e.Location).ToArray(),
                e.OldValue,
                e.NewValue
            )
        );
    }

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
