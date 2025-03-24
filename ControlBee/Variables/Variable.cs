using System.Text.Json;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Variables;

public class Variable<T> : ActorItem, IVariable, IWriteData, IDisposable
    where T : new()
{
    private static readonly ILog Logger = LogManager.GetLogger("Variable");
    private T _value;

    public Variable(VariableScope scope, T initialValue)
    {
        Scope = scope;
        _value = initialValue;
        OnAfterValueChange();
    }

    public Variable(IActorInternal actor, string itemPath, VariableScope scope, T value)
        : this(actor.VariableManager, actor, itemPath, scope, value) { }

    public Variable(
        IVariableManager variableManager,
        IActorInternal actor,
        string itemPath,
        VariableScope scope,
        T value
    )
        : this(scope, value)
    {
        Actor = actor;
        ItemPath = itemPath;
        variableManager.Add(this);
    }

    public Variable()
        : this(VariableScope.Global) { }

    public Variable(VariableScope scope)
        : this(scope, new T()) { }

    public Variable(IActorInternal actor, string itemPath, VariableScope scope)
        : this(actor, itemPath, scope, new T()) { }

    public string Unit { get; private set; } = string.Empty;

    public T Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            if (EqualityComparer<T>.Default.Equals(oldValue, value))
                return;
            Unsubscribe();
            _value = value;
            var newValue = _value;
            if (_value is ICloneable cloneable)
                newValue = (T)cloneable.Clone();
            OnAfterValueChange();
            OnValueChanged(new ValueChangedArgs([], oldValue, newValue));
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public object? ValueObject => Value;
    public VariableScope Scope { get; }

    public string ToJson()
    {
        CheckSanity();
        return JsonSerializer.Serialize(Value);
    }

    public void FromJson(string data)
    {
        var value = JsonSerializer.Deserialize<T>(data)!;
        if (value is IActorItemSub actorItemSub)
            actorItemSub.OnDeserialized();
        Value = value;
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemMetaDataRead":
            {
                var payload = new Dict
                {
                    [nameof(Name)] = Name,
                    [nameof(Unit)] = Unit,
                    [nameof(Desc)] = Desc,
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemMetaData", payload)
                );
                return true;
            }
            case "_itemDataRead":
            {
                var newValue = _value;
                if (_value is ICloneable cloneable)
                    newValue = (T)cloneable.Clone();
                var payload = new Dict
                {
                    [nameof(ValueChangedArgs)] = new ValueChangedArgs([], null, newValue),
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemDataChanged", payload)
                );
                return true;
            }
            case "_itemDataWrite":
            {
                WriteData((ItemDataWriteArgs)message.Payload!);
                return true;
            }
            default:
                if (!base.ProcessMessage(message))
                    throw new ValueError();
                break;
        }

        return false;
    }

    public override void UpdateSubItem()
    {
        if (_value is IActorItemSub subItem)
        {
            subItem.ItemPath = ItemPath;
            subItem.Actor = Actor;
            subItem.UpdateSubItem();
        }
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Unit)) is string unit)
            Unit = unit;
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        if (args.Location.Length == 0)
            Value = (T)args.NewValue;
        else
            (Value as IWriteData)?.WriteData(args);
    }

    private void OnAfterValueChange()
    {
        UpdateSubItem();
        Subscribe();
    }

    private void CheckSanity()
    {
        if (string.IsNullOrEmpty(ActorName) || string.IsNullOrEmpty(ItemPath))
            throw new ApplicationException("ActorName and ItemPath must not be empty.");
    }

    private void Subscribe()
    {
        if (_value is IValueChanged arrayValue)
            arrayValue.ValueChanged += ArrayValue_ValueChanged;
    }

    private void Unsubscribe()
    {
        if (_value is IValueChanged arrayValue)
            arrayValue.ValueChanged -= ArrayValue_ValueChanged;
    }

    private void ArrayValue_ValueChanged(object? sender, ValueChangedArgs e)
    {
        OnValueChanged(e);
    }

    protected virtual void OnValueChanged(ValueChangedArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
