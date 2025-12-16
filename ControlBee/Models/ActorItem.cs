using ControlBee.Interfaces;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ControlBee.Models;

public abstract class ActorItem : IActorItem, IActorItemModifier
{
    private string _name = string.Empty;
    private bool _visible = true;
    public string ActorName => Actor.Name;
    protected ITimeManager TimeManager => Actor.TimeManager;

    public string Name
    {
        get => string.IsNullOrEmpty(_name) ? ItemPath : _name;
        set => _name = value;
    }
    public string Desc { get; set; } = string.Empty;
    public bool Visible
    {
        get => _visible & VisibilityOverride;
        set
        {
            _visible = value;

            foreach (var field in GetType().GetFields())
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue is null) continue;
                if (fieldValue is IActorItemModifier actorItemModifier)
                    actorItemModifier.Visible = _visible;
            }
        }
    }
    public bool VisibilityOverride { get; set; } = true;

    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ItemPath { get; set; } = string.Empty;

    public virtual bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemMetaDataRead":
            {
                var payload = new Dictionary<string, object?>
                {
                    [nameof(Name)] = Name,
                    [nameof(Desc)] = Desc
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemMetaData", payload)
                );
                return true;
            }
        }

        return false;
    }

    public virtual void UpdateSubItem()
    {
    }

    public virtual void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Name)) is string name)
            _name = name;
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Desc)) is string desc)
            Desc = desc;
    }

    public virtual void Init()
    {
    }

    public virtual void PostInit()
    {
        // Empty
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void SetName(string name)
    {
        _name = name;
    }

    public void SetDesc(string desc)
    {
        Desc = desc;
    }
}