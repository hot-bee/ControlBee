using System.ComponentModel;
using System.Text.Json;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Variables;

public class Variable<T> : ActorItem, IVariable, IDisposable
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
            OnAfterValueChange();
            OnValueChanged(new ValueChangedEventArgs(null, oldValue, value));
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    public object? ValueObject => Value;
    public VariableScope Scope { get; }

    public string ToJson()
    {
        CheckSanity();
        return JsonSerializer.Serialize(Value);
    }

    public void FromJson(string data)
    {
        Value = JsonSerializer.Deserialize<T>(data)!;
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
                var payload = new Dict
                {
                    ["Location"] = null,
                    ["OldValue"] = null,
                    ["NewValue"] = _value,
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemDataChanged", payload)
                );
                return true;
            }
            case "_itemDataWrite":
            {
                var data = message.Payload!;
                Value = (T)data;
                return true;
            }
            case "_itemDataModify":
            {
                if (message.Payload! is not Dict data)
                {
                    Logger.Warn(
                        "Invalid payload: Expected a dictionary in _itemDataModify, but received a different type."
                    );
                    return false;
                }

                if (Value == null)
                {
                    Logger.Warn("Value is null while processing _itemDataModify.");
                    return false;
                }
                foreach (var (propertyName, propertyValue) in data)
                {
                    var propertyType = Value.GetType().GetProperty(propertyName);
                    if (propertyType == null)
                    {
                        Logger.Warn(
                            $"Property type couldn't be found by the name. ({propertyName})"
                        );
                        continue;
                    }
                    propertyType.SetValue(Value, propertyValue);
                }

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
        if (_value is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged += NotifyPropertyChangedOnPropertyChanged;
    }

    private void Unsubscribe()
    {
        if (_value is IValueChanged arrayValue)
            arrayValue.ValueChanged -= ArrayValue_ValueChanged;
        if (_value is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged -= NotifyPropertyChangedOnPropertyChanged;
    }

    private void ArrayValue_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        OnValueChanged(e);
    }

    private void NotifyPropertyChangedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var newValue =
            e.PropertyName != null
                ? sender?.GetType().GetProperty(e.PropertyName)?.GetValue(sender)
                : null;
        OnValueChanged(new ValueChangedEventArgs(e.PropertyName, null, newValue));
    }

    protected virtual void OnValueChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
