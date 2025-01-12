using System.Text.Json;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

public class Variable<T> : IVariable, IDisposable, IActorItem
    where T : new()
{
    private T _value;

    public Variable(VariableScope scope, T initialValue)
    {
        GroupName = string.Empty;
        Uid = string.Empty;
        Scope = scope;
        Value = initialValue;
        Subscribe();
    }

    public Variable(
        IVariableManager? variableManager,
        string groupName,
        string uid,
        VariableScope scope,
        T value
    )
        : this(scope, value)
    {
        if (variableManager == null)
            throw new ApplicationException(
                "A 'variableManager' instance must be provided to use Variable."
            );
        GroupName = groupName;
        Uid = uid;
        variableManager.Add(this);
    }

    public Variable()
        : this(VariableScope.Global) { }

    public Variable(VariableScope scope)
        : this(scope, new T()) { }

    public Variable(IActor actor, string uid, VariableScope scope)
        : this(actor.VariableManager, actor.ActorName, uid, scope, new T())
    {
        Actor = actor;
    }

    public Variable(IActor actor, string uid, VariableScope scope, T value)
        : this(actor.VariableManager, actor.ActorName, uid, scope, value)
    {
        Actor = actor;
    }

    public T Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            Unsubscribe();
            _value = value;
            UpdateSubItem();
            Subscribe();
            OnValueChanged(new ValueChangedEventArgs(null, oldValue, value));
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public IActor Actor { get; set; }

    public void UpdateSubItem()
    {
        if (_value is IActorItemSub subItem)
        {
            subItem.ItemName = Uid;
            subItem.Actor = Actor;
            subItem.UpdateSubItem();
        }
    }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    public object? ValueObject => Value;
    public VariableScope Scope { get; }
    public string GroupName { get; set; }
    public string Uid { get; set; }

    public string ToJson()
    {
        CheckSanity();
        return JsonSerializer.Serialize(Value);
    }

    public void FromJson(string data)
    {
        Value = JsonSerializer.Deserialize<T>(data)!;
    }

    private void CheckSanity()
    {
        if (string.IsNullOrEmpty(GroupName) || string.IsNullOrEmpty(Uid))
            throw new ApplicationException("GroupName and Uid must not be empty.");
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

    private void ArrayValue_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        OnValueChanged(e);
    }

    protected virtual void OnValueChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
