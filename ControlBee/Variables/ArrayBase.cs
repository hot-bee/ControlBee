using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Variables;

public abstract class ArrayBase : IValueChanged, IActorItemSub, ICloneable
{
    public event EventHandler<ValueChangedArgs>? ValueChanged;
    public abstract void ReadJson(JsonDocument jsonDoc);
    public abstract void WriteJson(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    );

    protected virtual void OnArrayElementChanged(ValueChangedArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
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

    public virtual void OnDeserialized() { }

    public bool ProcessMessage(ActorItemMessage message)
    {
        // Empty
        return true;
    }

    public abstract object Clone();
}
