using System.Text.Json;
using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Variables;

public abstract class ArrayBase : INotifyValueChanged, IActorItemSub, ICloneable
{
    public event EventHandler<ValueChangedArgs>? ValueChanging;
    public event EventHandler<ValueChangedArgs>? ValueChanged;

    [Obsolete]
    public abstract void ReadJson(JsonDocument jsonDoc);

    [Obsolete]
    public abstract void WriteJson(
        Utf8JsonWriter writer,
        ArrayBase value,
        JsonSerializerOptions options
    );

    [JsonIgnore]
    public IActorInternal Actor { get; set; } = EmptyActor.Instance;

    [JsonIgnore]
    public string ItemPath { get; set; } = string.Empty;

    [JsonIgnore]
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

    public abstract bool ProcessMessage(ActorItemMessage message);

    public abstract object Clone();

    protected virtual void OnValueChanged(ValueChangedArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    protected virtual void OnValueChanging(ValueChangedArgs e)
    {
        ValueChanging?.Invoke(this, e);
    }
}
