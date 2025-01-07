using System.Text.Json;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

public abstract class ArrayBase : IValueChanged
{
    public event EventHandler<ValueChangedEventArgs>? ValueChanged;
    public abstract void ReadJson(JsonDocument jsonDoc);
    public abstract void WriteJson(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    );

    protected virtual void OnArrayElementChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    protected static void CheckType(Type elementType)
    {
        if (elementType == typeof(String))
            throw new ApplicationException(
                "String (capitalized) is not allowed as the element type of an array. Use the primitive string type instead."
            );
    }
}
