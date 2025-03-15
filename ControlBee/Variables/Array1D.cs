﻿using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Variables;

[JsonConverter(typeof(ArrayBaseConverter))]
public class Array1D<T> : ArrayBase, IArray1D, IDisposable, IWriteData
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
            Unsubscribe(x);
            _value[x] = value;
            Subscribe(x);
            OnArrayElementChanged(new ValueChangedArgs([x], oldValue, value));
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

    private void Subscribe(int index)
    {
        if (_value[index] is IValueChanged valueChanged)
            valueChanged.ValueChanged += ValueChangedOnValueChanged;
    }

    private void Unsubscribe(int index)
    {
        if (_value[index] is IValueChanged valueChanged)
            valueChanged.ValueChanged -= ValueChangedOnValueChanged;
    }

    private void ValueChangedOnValueChanged(object? sender, ValueChangedArgs e)
    {
        var index = Array.IndexOf(_value, sender);
        if (index == -1)
        {
            Logger.Warn($"Couldn't find index of the changed value. ({sender})");
            return;
        }

        OnArrayElementChanged(
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
