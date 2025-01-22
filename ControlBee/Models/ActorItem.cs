using ControlBee.Interfaces;

namespace ControlBee.Models;

public abstract class ActorItem : IActorItem
{
    private string _name = string.Empty;
    public string ActorName => Actor.Name;
    protected ITimeManager TimeManager => Actor.TimeManager;

    public string Name => string.IsNullOrEmpty(_name) ? ItemPath : _name;
    public string Desc { get; private set; } = string.Empty;

    public IActorInternal Actor { get; set; } = EmptyActor.Instance;
    public string ItemPath { get; set; } = string.Empty;

    public virtual bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemMetaDataRead":
            {
                var payload = new Dictionary<string, object>
                {
                    [nameof(Name)] = Name,
                    [nameof(Desc)] = Desc,
                };
                message.Sender.Send(
                    new ActorItemMessage(message.Id, Actor, ItemPath, "_itemMetaData", payload)
                );
                return true;
            }
        }

        return false;
    }

    public abstract void UpdateSubItem();

    public virtual void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Name)) is string name)
            _name = name;
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Desc)) is string desc)
            Desc = desc;
    }
}
