using System.Text.Json;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

public abstract class ArrayBase : IValueChanged, IActorItemSub
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

    public IActorInternal Actor { get; set; } = Models.Actor.Empty;
    public string ItemPath { get; set; } = string.Empty;
    public abstract IEnumerable<object?> Items { get; }

    public void UpdateSubItem()
    {
        foreach (var item in Items)
        {
            if (item is not IActorItemSub itemSub)
                continue;
            itemSub.Actor = Actor;
            itemSub.ItemPath = ItemPath;
            itemSub.UpdateSubItem();
        }
    }
}
