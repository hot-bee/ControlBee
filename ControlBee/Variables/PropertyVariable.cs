using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Variables;

public abstract class PropertyVariable : IActorItemSub, IValueChanged, IWriteData
{
    private static readonly ILog Logger = LogManager.GetLogger("PropertyVariable");
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

    [JsonIgnore]
    public IActorInternal Actor { get; set; } = null!;

    [JsonIgnore]
    public string ItemPath { get; set; } = null!;

    public void UpdateSubItem()
    {
        // TODO
    }

    public abstract void OnDeserialized();
}
