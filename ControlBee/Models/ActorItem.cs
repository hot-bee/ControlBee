using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

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
        get => _visible;
        set
        {
            _visible = value;

            foreach (var field in GetType().GetFields())
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue is null)
                    continue;
                if (fieldValue is IActorItemModifier actorItemModifier)
                    actorItemModifier.Visible = _visible;
            }
        }
    }

    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ItemPath { get; set; } = string.Empty;

    public virtual bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemMetaDataRead":
                SendMetaData(message.Id);
                return true;
        }

        return false;
    }

    public virtual void UpdateSubItem() { }

    public virtual void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        _name = dataSource.GetValue(ActorName, ItemPath, nameof(Name)) as string ?? string.Empty;
        Desc = dataSource.GetValue(ActorName, ItemPath, nameof(Desc)) as string ?? string.Empty;
    }

    public virtual void ReloadProperties(ISystemPropertiesDataSource dataSource)
    {
        InjectProperties(dataSource);
        SendMetaData();
    }

    protected virtual void SendMetaData(Guid requestId = default)
    {
        if (Actor.Ui is not IUiActor uiActor)
            return;
        var payload = new Dictionary<string, object?>
        {
            [nameof(Name)] = Name,
            [nameof(Desc)] = Desc,
        };
        uiActor.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemMetaDataChanged", payload)
        );
    }

    public virtual void Init() { }

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
