using System.Collections.Concurrent;
using System.Reflection;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Utils;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class Actor : IActorInternal, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    private readonly IActorItemInjectionDataSource _actorItemInjectionDataSource;

    private readonly Dictionary<string, IActorItem> _actorItems = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly PlaceholderManager _placeholderManager = new();
    private readonly Thread _thread;

    protected readonly ActorBuiltinMessageHandler ActorBuiltinMessageHandler;

    private bool _init;

    private IState _initialState;

    private string _title = string.Empty;
    public PlatformException? ExitError;

    public Dictionary<string, IActor> PeerDict = [];
    public Dictionary<IActor, Dict> PeerStatus = new();

    public IState State;
    public Dict Status = new();

    public Actor(ActorConfig config)
    {
        Logger.Info($"Creating an instance of Actor. ({config.ActorName})");
        _thread = new Thread(RunThread);

        SkipWaitSensor = config.SystemConfigurations.SkipWaitSensor;
        VariableManager = config.VariableManager;
        TimeManager = config.TimeManager;
        _actorItemInjectionDataSource = config.ActorItemInjectionDataSource;
        PositionAxesMap = new PositionAxesMap();
        Name = config.ActorName;
        Ui = config.UiActor;

        State = new EmptyState(this);
        _initialState = State;

        ActorBuiltinMessageHandler = new ActorBuiltinMessageHandler(this);
        _mailbox.Add(new OnStateEntryMessage(this));
    }

    public bool SkipWaitSensor { get; }

    public string Name { get; }

    public string Title
    {
        get
        {
            if (string.IsNullOrEmpty(_title))
                return Name;
            return _title;
        }
    }

    public virtual Guid Send(Message message) // TODO: Remove virtual
    {
        _mailbox.Add(message);
        return message.Id;
    }

    public (string itemPath, Type type)[] GetItems()
    {
        return _actorItems.ToList().ConvertAll(x => (x.Key, x.Value.GetType())).ToArray();
    }

    public ITimeManager TimeManager { get; }
    public IActor? Ui { get; }

    public IPositionAxesMap PositionAxesMap { get; }
    public IVariableManager VariableManager { get; }

    public void Init(ActorConfig config)
    {
        if (_init)
            throw new ApplicationException();
        _init = true;

        _initialState = State;
        IterateItems(string.Empty, this, InitItem, config);
        IterateItems(string.Empty, this, ReplacePlaceholder, config);
        PositionAxesMap.UpdateMap();
    }

    public IActorItem? GetItem(string itemPath)
    {
        if (!itemPath.StartsWith("/"))
            itemPath = "/" + itemPath;
        return _actorItems.GetValueOrDefault(itemPath);
    }

    public virtual string[] GetFunctions()
    {
        return [];
    }

    public void Dispose()
    {
        Logger.Info("Releasing resources for Actor instance.");
        if (_thread.ThreadState != ThreadState.Unstarted)
        {
            _cancellationTokenSource.Cancel();
            _thread.Join();
        }

        Logger.Info("Actor instance successfully disposed.");
    }

    public void PublishStatus()
    {
        var clonedStatus = DictCopy.Copy(Status);
        foreach (var peer in PeerDict.Values)
            peer.Send(new Message(this, "_status", clonedStatus));
    }

    public void SetStatus(string name, object? value)
    {
        Status[name] = value;
        PublishStatus();
    }

    public void SetStatusByActor(string actorName, string keyName, object? value)
    {
        var statusByActor = Status.GetValueOrDefault(actorName) as Dict ?? new Dict();
        statusByActor[keyName] = value;
        Status[actorName] = statusByActor;
        PublishStatus();
    }

    public void SetStatusByActor(IActor actor, string keyName, object? value)
    {
        SetStatusByActor(actor.Name, keyName, value);
    }

    public object? GetPeerStatus(IActor actor, string keyName)
    {
        return PeerStatus[actor].GetValueOrDefault(keyName);
    }

    public object? GetPeerStatus(string actorName, string keyName)
    {
        return GetPeerStatus(PeerDict[actorName], keyName);
    }

    public object? GetPeerStatusByActor(IActor actor, string keyName)
    {
        return (GetPeerStatus(actor, Name) as Dict)?.GetValueOrDefault(keyName);
    }

    public object? GetPeerStatusByActor(string actorName, string keyName)
    {
        return GetPeerStatusByActor(PeerDict[actorName], keyName);
    }

    public void ResetState()
    {
        State = _initialState;
    }

    public event EventHandler<(
        Message message,
        IState oldState,
        IState newState,
        bool result
    )>? MessageProcessed;

    public void SetTitle(string title)
    {
        _title = title;
    }

    private void IterateItems(
        string itemPathPrefix,
        object actorItemHolder,
        Func<object, IActorItem, FieldInfo, string, ActorConfig, IActorItem> func,
        ActorConfig config
    )
    {
        var fieldInfos = actorItemHolder.GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IActorItem)))
            {
                var itemPath = string.Join('/', itemPathPrefix, fieldInfo.Name);
                var actorItem = (IActorItem)fieldInfo.GetValue(actorItemHolder)!;

                actorItem = func(actorItemHolder, actorItem, fieldInfo, itemPath, config);

                IterateItems(itemPath, actorItem, func, config);
            }
    }

    private IActorItem InitItem(
        object actorItemHolder,
        IActorItem actorItem,
        FieldInfo fieldInfo,
        string itemPath,
        ActorConfig config
    )
    {
        if (actorItem is IPlaceholder placeHolder)
        {
            IActorItem newItem;
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IDigitalInput)))
                newItem = config.DigitalInputFactory.Create();
            else if (fieldInfo.FieldType.IsAssignableTo(typeof(IDigitalOutput)))
                newItem = config.DigitalOutputFactory.Create();
            else
                throw new ValueError();
            _placeholderManager.Add(placeHolder, newItem);
            actorItem = newItem;
            fieldInfo.SetValue(actorItemHolder, actorItem);
        }

        AddItem(actorItem, itemPath);
        actorItem.InjectProperties(_actorItemInjectionDataSource);
        return actorItem;
    }

    private IActorItem ReplacePlaceholder(
        object actorItemHolder,
        IActorItem actorItem,
        FieldInfo fieldInfo,
        string itemPath,
        ActorConfig config
    )
    {
        if (actorItem is IUsesPlaceholder usesPlaceholder)
            usesPlaceholder.ReplacePlaceholder(_placeholderManager);
        return actorItem;
    }

    public void AddItem(IActorItem actorItem, string itemPath)
    {
        actorItem.Actor = this;
        actorItem.ItemPath = itemPath;
        actorItem.UpdateSubItem();
        _actorItems[itemPath] = actorItem;

        if (actorItem is IVariable variable)
            VariableManager.Add(variable);
    }

    public virtual void Start()
    {
        Logger.Info($"Starting Actor instance. ({Name})");
        _thread.Name = $"Actor_{Name}";
        _thread.Start();
    }

    private void RunThread()
    {
        TimeManager.Register();
        try
        {
            while (true)
            {
                var message = _mailbox.Take(_cancellationTokenSource.Token);
                if (message.Name == "_terminate")
                    break;
                MessageHandler(message);
            }
        }
        catch (OperationCanceledException e)
        {
            Logger.Info(e);
        }
        catch (PlatformException e)
        {
            Logger.Error(e);
            ExitError = e;
        }
        finally
        {
            TimeManager.Unregister();
        }
    }

    protected virtual bool ProcessMessage(Message message)
    {
        var result = ActorBuiltinMessageHandler.ProcessMessage(message);
        result |= State.ProcessMessage(message);
        return result;
    }

    protected virtual void MessageHandler(Message message)
    {
        try
        {
            if (message is ActorItemMessage actorItemMessage)
            {
                var item = GetItem(actorItemMessage.ItemPath);
                var result = item?.ProcessMessage(actorItemMessage) ?? false;
                if (!result)
                    actorItemMessage.Sender.Send(new DroppedMessage(message.Id, this));
                return;
            }

            while (true)
            {
                var oldState = State;
                var result = ProcessMessage(message);
                OnMessageProcessed((message, oldState, State, result));
                if (oldState != State)
                {
                    if (!result)
                        throw new PlatformException(
                            "State has changed but ProcessMessage() returns false."
                        );
                    oldState.Dispose();
                    message = new OnStateEntryMessage(this);
                    Ui?.Send(new Message(this, "_stateChanged", State.GetType().Name));
                    continue;
                }

                if (
                    !result
                    && message.GetType() != typeof(DroppedMessage)
                    && message.GetType() != typeof(OnStateEntryMessage)
                )
                    message.Sender.Send(new DroppedMessage(message.Id, this));
                break;
            }
        }
        catch (FatalSequenceError)
        {
            State = _initialState;
        }
    }

    public void Join()
    {
        _thread.Join();
    }

    public void InitPeers(IActor[] peerList)
    {
        if (Ui != null && !peerList.Contains(Ui))
            peerList = peerList.Concat([Ui]).ToArray();
        foreach (var peer in peerList)
        {
            if (!PeerDict.TryAdd(peer.Name, peer))
                throw new ValueError("Duplicate name.");
            PeerStatus[peer] = new Dictionary<string, object?>();
        }
    }

    private void OnMessageProcessed(
        (Message message, IState oldState, IState newState, bool result) e
    )
    {
        MessageProcessed?.Invoke(this, e);
    }
}
