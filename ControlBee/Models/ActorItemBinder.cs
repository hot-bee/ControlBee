using ControlBee.Interfaces;
using ControlBee.Variables;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class ActorItemBinder : IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger("General");
    private static int _instanceCount;
    private readonly string _actorName;
    private readonly Guid? _itemMetaDataReadMessageId;
    private readonly string _itemPath;
    private readonly IUiActor _uiActor;

    public ActorItemBinder(IActorRegistry actorRegistry, string actorName, string itemPath)
    {
        _actorName = actorName;
        _itemPath = itemPath;
        _uiActor = (IUiActor)actorRegistry.Get("Ui")!;
        var actor = actorRegistry.Get(actorName)!;
        _uiActor.MessageArrived += _uiActor_MessageArrived;
        _itemMetaDataReadMessageId = actor.Send(
            new ActorItemMessage(_uiActor, _itemPath, "_itemMetaDataRead")
        );
        actor.Send(new ActorItemMessage(_uiActor, _itemPath, "_itemDataRead"));
        _instanceCount++;
        Logger.Debug($"Active Instance Count: {_instanceCount}");
    }

    public void Dispose()
    {
        _uiActor.MessageArrived -= _uiActor_MessageArrived;
        _instanceCount--;
    }

    public event EventHandler<Dict>? MetaDataChanged;
    public event EventHandler<Dict>? DataChanged;
    public event EventHandler<Dict>? ErrorOccurred;

    private void _uiActor_MessageArrived(object? sender, Message e)
    {
        if (e.RequestId == _itemMetaDataReadMessageId && e.Name == "_itemMetaData")
            OnMetaDataChanged((Dict)e.Payload!);

        if (e.Name == "_itemDataChanged")
        {
            var actorItemMessage = (ActorItemMessage)e;
            if (actorItemMessage.ActorName == _actorName && actorItemMessage.ItemPath == _itemPath)
                OnDataChanged((Dict)e.Payload!);
        }

        if (e.Name == "_errorItemDataWrite")
        {
            var actorItemMessage = (ActorItemMessage)e;
            if (actorItemMessage.ActorName == _actorName && actorItemMessage.ItemPath == _itemPath)
                OnErrorOccurred((Dict)e.Payload!);
        }
    }

    protected virtual void OnDataChanged(Dict e)
    {
        DataChanged?.Invoke(this, e);
    }

    protected virtual void OnMetaDataChanged(Dict e)
    {
        MetaDataChanged?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(Dict e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}
