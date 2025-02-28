using System.Reflection;
using ControlBee.Interfaces;
using log4net;

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
        _uiActor = (IUiActor)actorRegistry.Get("ui")!;
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

    public event EventHandler<Dictionary<string, object?>>? MetaDataChanged;
    public event EventHandler<Dictionary<string, object?>>? DataChanged;

    private void _uiActor_MessageArrived(object? sender, Message e)
    {
        if (e.RequestId == _itemMetaDataReadMessageId && e.Name == "_itemMetaData")
            OnMetaDataChanged((Dictionary<string, object?>)e.Payload!);

        if (e.Name == "_itemDataChanged")
        {
            var actorItemMessage = (ActorItemMessage)e;
            if (actorItemMessage.ActorName == _actorName && actorItemMessage.ItemPath == _itemPath)
                OnDataChanged((Dictionary<string, object?>)e.Payload!);
        }
    }

    protected virtual void OnDataChanged(Dictionary<string, object?> e)
    {
        DataChanged?.Invoke(this, e);
    }

    protected virtual void OnMetaDataChanged(Dictionary<string, object?> e)
    {
        MetaDataChanged?.Invoke(this, e);
    }
}
