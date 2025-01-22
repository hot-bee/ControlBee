using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBee.Variables;

namespace ControlBee.Models;

public class ActorItemBinder
{
    private readonly IActor _actor;
    private readonly string _actorName;
    private readonly string _itemPath;
    private readonly IUiActor _uiActor;
    private Guid? _sentMessageId;

    public ActorItemBinder(IActorRegistry actorRegistry, string actorName, string itemPath)
    {
        _actorName = actorName;
        _itemPath = itemPath;
        _uiActor = (IUiActor)actorRegistry.Get("ui");
        _actor = actorRegistry.Get(actorName);
        _uiActor.MessageArrived += _uiActor_MessageArrived;
        _sentMessageId = _actor.Send(new ActorItemMessage(_uiActor, _itemPath, "_itemDataRead"));
    }

    public event EventHandler<ValueChangedEventArgs>? DataChanged;

    private void _uiActor_MessageArrived(object? sender, Message e)
    {
        if (e.RequestId == _sentMessageId && e.Name == "_itemData")
        {
            OnDataChanged((ValueChangedEventArgs)e.Payload!);
            _sentMessageId = null;
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
}
