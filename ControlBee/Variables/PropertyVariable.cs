using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;

namespace ControlBee.Variables;

public abstract class PropertyVariable : IActorItemSub, INotifyValueChanged, IWriteData
{
    private static readonly ILog Logger = LogManager.GetLogger("PropertyVariable");

    protected PropertyVariable()
    {
    }

    protected PropertyVariable(PropertyVariable source)
    {
        Actor = source.Actor;
        ItemPath = source.ItemPath;
    }

    [JsonIgnore] public IActorInternal Actor { get; set; } = null!;

    [JsonIgnore] public string ItemPath { get; set; } = null!;

    public void UpdateSubItem()
    {
        // TODO
    }

    public abstract void OnDeserialized();

    public bool ProcessMessage(ActorItemMessage message)
    {
        // Empty
        return false;
    }

    public event EventHandler<ValueChangedArgs>? ValueChanging;
    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public virtual void WriteData(ItemDataWriteArgs args)
    {
        var propertyName = (string)args.Location[0];
        var propertyType = GetType().GetProperty(propertyName);
        if (propertyType == null)
        {
            Logger.Warn($"Property type couldn't be found by the name. ({propertyName})");
            return;
        }

        if (propertyType.PropertyType.IsAssignableTo(typeof(IIndex2D)))
        {
            var location = (ValueTuple<int, int>)args.Location[1];
            var propertyValue = (IIndex2D)propertyType.GetValue(this)!;
            propertyValue.SetValue(location.Item1, location.Item2, args.NewValue);
            return;
        }

        if (args.Location.Length == 1)
            propertyType.SetValue(this, args.NewValue);
        else
            (propertyType.GetValue(this) as IWriteData)?.WriteData(
                new ItemDataWriteArgs(args.Location[1..], args.NewValue)
            );
    }

    protected virtual void OnValueChanged(ValueChangedArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    protected virtual void OnValueChanging(ValueChangedArgs e)
    {
        ValueChanging?.Invoke(this, e);
    }
}