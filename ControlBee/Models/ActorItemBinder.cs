using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Models;

public class ActorItemBinder
{
    private readonly string _actorName;
    private readonly string _itemPath;
    private readonly IUiActor _uiActor;
    private readonly Guid? _itemMetaDataReadMessageId;
    private readonly Guid? _itemDataReadMessageId;

    public ActorItemBinder(IActorRegistry actorRegistry, string actorName, string itemPath)
    {
        _actorName = actorName;
        _itemPath = itemPath;
        _uiActor = (IUiActor)actorRegistry.Get("ui")!;
        var actor = actorRegistry.Get(actorName)!;
        _uiActor.MessageArrived += _uiActor_MessageArrived;
        _itemMetaDataReadMessageId = actor.Send(
            new ActorItemMessage(_uiActor, _itemPath, "_itemMetaDataRead")
        );
        _itemDataReadMessageId = actor.Send(
            new ActorItemMessage(_uiActor, _itemPath, "_itemDataRead")
        );
    }

    public event EventHandler<Dictionary<string, object>>? MetaDataChanged;
    public event EventHandler<ValueChangedEventArgs>? DataChanged;

    private void _uiActor_MessageArrived(object? sender, Message e)
    {
        if (e.RequestId == _itemMetaDataReadMessageId && e.Name == "_itemMetaData")
        {
            OnMetaDataChanged((Dictionary<string, object>)e.Payload!);
        }

        if (e.RequestId == _itemDataReadMessageId && e.Name == "_itemData")
        {
            OnDataChanged((ValueChangedEventArgs)e.Payload!);
        }

        if (e.Name == "_itemDataChanged")
        {
            var actorItemMessage = (ActorItemMessage)e;
            if (actorItemMessage.ActorName == _actorName && actorItemMessage.ItemPath == _itemPath)
                OnDataChanged((ValueChangedEventArgs)e.Payload!);
        }
    }

    protected virtual void OnDataChanged(ValueChangedEventArgs e)
    {
        DataChanged?.Invoke(this, e);
    }

    protected virtual void OnMetaDataChanged(Dictionary<string, object> e)
    {
        MetaDataChanged?.Invoke(this, e);
    }
}
