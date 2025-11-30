using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using ControlBeeAbstract.Exceptions;
using log4net;
using Newtonsoft.Json;
using Dict = System.Collections.Generic.Dictionary<string, object?>;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ControlBee.Variables;

public class Variable : ActorItem
{
    protected static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new RespectSystemTextJsonIgnoreResolver(),
        Formatting = Formatting.Indented
    };
}

public class Variable<T> : Variable, IVariable, IWriteData, IDisposable
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
        : this(actor.VariableManager, actor, itemPath, scope, value)
    {
    }

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
        : this(VariableScope.Global)
    {
    }

    public Variable(VariableScope scope)
        : this(scope, new T())
    {
    }

    public Variable(IActorInternal actor, string itemPath, VariableScope scope)
        : this(actor, itemPath, scope, new T())
    {
    }

    public string Unit { get; private set; } = string.Empty;
    public int? ReadLevel { get; private set; }
    public int? WriteLevel { get; private set; }
    public double? MinValue { get; private set; }
    public double? MaxValue { get; private set; }

    public T Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            if (EqualityComparer<T>.Default.Equals(oldValue, value))
                return;
            var newValue = value;
            if (value is ICloneable cloneable)
                newValue = (T)cloneable.Clone();
            var valueChangedArgs =
                new ValueChangedArgs([], oldValue, newValue); // TODO: Not sure why we need to cloned value here.
            OnValueChanging(valueChangedArgs);
            Unsubscribe();
            _value = value;
            OnAfterValueChange();
            OnValueChanged(valueChangedArgs);
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public event EventHandler<ValueChangedArgs>? ValueChanging;
    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public int? Id { get; set; }

    public object? ValueObject
    {
        get => Value;
        set => Value = (T)value!;
    }

    public VariableScope Scope { get; }

    public IUserInfo? UserInfo { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(new DataV2(DataV2.ValidVersion, Value), JsonSettings);
    }

    public bool Dirty { get; set; }

    public void FromJson(string data)
    {
        try
        {
            var value = JsonConvert.DeserializeObject<DataV2>(data)!;
            if (value.Version != DataV2.ValidVersion) throw new JsonException();
            if (value.Value is IActorItemSub actorItemSub)
                actorItemSub.OnDeserialized();
            Value = value.Value;
        }
        catch (JsonException)
        {
            // Fallback to previous format.
            var value = JsonSerializer.Deserialize<T>(data)!;
            if (value is IActorItemSub actorItemSub)
                actorItemSub.OnDeserialized();
            Value = value;
            throw new FallbackException();
        }
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
                    [nameof(MinValue)] = MinValue,
                    [nameof(MaxValue)] = MaxValue,
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemMetaData", payload)
                );
                return true;
            }
            case "_itemDataRead":
            {
                if (ReadLevel.HasValue && UserInfo != null)
                    if (ReadLevel.Value < UserInfo.Level)
                        return false;
                var newValue = _value;
                if (_value is ICloneable cloneable)
                    newValue = (T)cloneable.Clone();
                var payload = new Dict
                {
                    [nameof(ValueChangedArgs)] = new ValueChangedArgs([], null, newValue)
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemDataChanged", payload)
                );
                return true;
            }
            case "_itemDataWrite":
            {
                if (WriteLevel.HasValue && UserInfo != null)
                    if (WriteLevel.Value < UserInfo.Level)
                        return false;
                var args = new ItemDataWriteArgs((ItemDataWriteArgs)message.Payload!)
                {
                    MinValue = MinValue,
                    MaxValue = MaxValue
                };
                try
                {
                    WriteData(args);
                }
                catch (ValueError error)
                {
                    Logger.Warn(error);
                }
                return true;
            }
        }

        var ret = base.ProcessMessage(message);
        if (_value is IActorItemSub actorItemSub)
            actorItemSub.ProcessMessage(message);

        return ret;
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
        if (dataSource.GetValue(ActorName, ItemPath, nameof(ReadLevel)) is string readLevel)
            ReadLevel = int.Parse(readLevel);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(WriteLevel)) is string writeLevel)
            WriteLevel = int.Parse(writeLevel);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(MinValue)) is string minValue)
            MinValue = double.Parse(minValue);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(MaxValue)) is string maxValue)
            MaxValue = double.Parse(maxValue);
    }

    public void WriteData(ItemDataWriteArgs args)
    {
        if (args.Location.Length == 0)
        {
            args.EnsureNewValueInRange();
            Value = (T)args.NewValue;
        }
        else
        {
            (Value as IWriteData)?.WriteData(args);
        }
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
        if (_value is INotifyValueChanged notify)
        {
            notify.ValueChanging += NotifyOnValueChanging;
            notify.ValueChanged += NotifyOnValueChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_value is INotifyValueChanged notify)
        {
            notify.ValueChanging -= NotifyOnValueChanging;
            notify.ValueChanged -= NotifyOnValueChanged;
        }
    }

    private void NotifyOnValueChanging(object? sender, ValueChangedArgs e)
    {
        OnValueChanging(e);
    }

    private void NotifyOnValueChanged(object? sender, ValueChangedArgs e)
    {
        OnValueChanged(e);
    }

    protected virtual void OnValueChanged(ValueChangedArgs e)
    {
        Dirty = true;
        ValueChanged?.Invoke(this, e);
    }

    protected virtual void OnValueChanging(ValueChangedArgs e)
    {
        ValueChanging?.Invoke(this, e);
    }

    private record DataV2(int Version, T Value)
    {
        public const int ValidVersion = 2;
    }
}