using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class ActorItem : IActorItem
{
    private string _name = string.Empty;
    public string ActorName => Actor.Name;
    protected ITimeManager TimeManager => Actor.TimeManager;

    public string Name => string.IsNullOrEmpty(_name) ? ItemPath : _name;
    public string Desc { get; private set; } = string.Empty;
    public bool Visible { get; set; } = true;

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
}