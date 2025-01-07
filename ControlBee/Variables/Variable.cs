using System.Text.Json;
using ControlBee.Interfaces;

namespace ControlBee.Variables;

public class Variable<T> : IVariable, IDisposable
    where T : new()
{
    private T _value;

    public Variable(
        IVariableManager? variableManager,
        string groupName,
        string uid,
        VariableScope scope,
        T value
    )
    {
        if (variableManager == null)
            throw new ApplicationException(
                "A 'variableManager' instance must be provided to use Variable."
            );
        GroupName = groupName;
        Uid = uid;
        Scope = scope;
        _value = value;
        Subscribe();
        variableManager.Add(this);
    }

    public Variable(IActor actor, string uid, VariableScope scope)
        : this(actor.VariableManager, actor.ActorName, uid, scope, new T()) { }

    public Variable(IActor actor, string uid, VariableScope scope, T value)
        : this(actor.VariableManager, actor.ActorName, uid, scope, value) { }

    public T Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            Unsubscribe();
            _value = value;
            Subscribe();
            OnValueChanged(new ValueChangedEventArgs(null, oldValue, value));
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    public VariableScope Scope { get; }
    public string GroupName { get; }
    public string Uid { get; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(Value);
    }

    public void FromJson(string data)
    {
        Value = JsonSerializer.Deserialize<T>(data)!;
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
