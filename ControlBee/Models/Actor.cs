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
    private static readonly ILog Logger = LogManager.GetLogger("General");
    private static readonly ILog StateLogger = LogManager.GetLogger("State");
    private static readonly ILog MessageLogger = LogManager.GetLogger("Message");
    private static readonly ILog StatusLogger = LogManager.GetLogger("Status");

    private readonly Dictionary<string, IActorItem> _actorItems = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly PlaceholderManager _placeholderManager = new();

    private readonly Stack<IState> _stateStack = new(new List<IState> { new EmptyState() });
    private readonly ISystemPropertiesDataSource _systemPropertiesDataSource;
    private readonly Thread _thread;

    protected readonly ActorBuiltinMessageHandler ActorBuiltinMessageHandler;

    private bool _init;

    private IState _initialState;

    private string _title = string.Empty;

    public PlatformException? LastPlatformException;

    public Dictionary<string, IActor> PeerDict = [];
    public Dictionary<IActor, Dict> PeerStatus = new();

    public Dict Status = new();

    public Actor(ActorConfig config)
    {
        Logger.Info($"Creating an instance of Actor. ({config.ActorName})");
        _thread = new Thread(RunThread);

        SkipWaitSensor = config.SystemConfigurations.SkipWaitSensor;
        VariableManager = config.VariableManager;
        TimeManager = config.TimeManager;
        ScenarioFlowTester = config.ScenarioFlowTester;
        _systemPropertiesDataSource = config.SystemPropertiesDataSource;
        PositionAxesMap = new PositionAxesMap();
        Name = config.ActorName;
        Ui = config.UiActor;

        _initialState = State;

        ActorBuiltinMessageHandler = new ActorBuiltinMessageHandler(this);
        _mailbox.Add(new StateEntryMessage(this));
    }

    public IState State
    {
        get => _stateStack.Peek();
        set
        {
            if (_stateStack.Count > 1)
                Logger.Warn(
                    "State Stack Count is greater than 1. However, it's setting an state over it."
                );
            _stateStack.Clear();
            _stateStack.Push(value);
        }
    }

    public int StateStackCount => _stateStack.Count;

    public bool SkipWaitSensor { get; }

    public int MessageFetchTimeout { get; set; } = -1;

    public IScenarioFlowTester ScenarioFlowTester { get; }

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

        var content =
            $"{message.Sender.Name}->{Name}: {message.Name} ({message.Id.ToString()[..6]},{message.RequestId.ToString()[..6]})";
        if (message.Name == "_status")
            MessageLogger.Debug(content);
        else
            MessageLogger.Info(content);
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

    public virtual void Init(ActorConfig config)
    {
        if (_init)
        {
            Logger.Warn("Init is already done.");
            return;
        }

        _init = true;

        _initialState = State;
        IterateItems(string.Empty, this, InitItem, config);
        IterateItems(string.Empty, this, ReplacePlaceholder, config);
        PositionAxesMap.UpdateMap();

        UpdateTitle();
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

    public string[] GetAxisItemPaths(string positionItemPath)
    {
        return PositionAxesMap.Get(positionItemPath).ToList().ConvertAll(x => x.ItemPath).ToArray();
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

    public void PushState(IState state)
    {
        _stateStack.Push(state);
    }

    public void PopState()
    {
        _stateStack.Pop();
    }

    public bool TryPopState()
    {
        if (StateStackCount <= 1)
            return false;
        _stateStack.Pop();
        return true;
    }

    private void UpdateTitle()
    {
        var title = GetProperty("/Name") as string;
        if (!string.IsNullOrEmpty(title))
            SetTitle(title);
    }

    public void PublishStatus()
    {
        var clonedStatus = DictCopy.Copy(Status);
        foreach (var peer in PeerDict.Values)
            peer.Send(new Message(this, "_status", clonedStatus));
    }

    public void SetStatus(string name, object? value)
    {
        if (value != null && Status.TryGetValue(name, out var oldValue))
            if (value.Equals(oldValue))
                return;
        StatusLogger.Info($"SetStatus: {name}, {value}");
        Status[name] = value;
        PublishStatus();
    }

    public object? GetStatus(string name)
    {
        return Status.GetValueOrDefault(name);
    }

    public void SetStatusByActor(string actorName, string keyName, object? value)
    {
        var statusByActor = Status.GetValueOrDefault(actorName) as Dict ?? new Dict();
        if (value != null && statusByActor.TryGetValue(keyName, out var oldValue))
            if (value.Equals(oldValue))
                return;
        StatusLogger.Info($"SetStatusByActor: {actorName}, {keyName}, {value}");
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

    public event EventHandler<(IState oldState, IState newState)>? StateChanged;

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
            else if (fieldInfo.FieldType.IsAssignableTo(typeof(IAnalogInput)))
                newItem = config.AnalogInputFactory.Create();
            else if (fieldInfo.FieldType.IsAssignableTo(typeof(IAnalogOutput)))
                newItem = config.AnalogOutputFactory.Create();
            else if (fieldInfo.FieldType.IsAssignableTo(typeof(IDialog)))
                newItem = config.DialogFactory.Create();
            else
                throw new ValueError();
            _placeholderManager.Add(placeHolder, newItem);
            actorItem = newItem;
            fieldInfo.SetValue(actorItemHolder, actorItem);
        }

        AddItem(actorItem, itemPath);
        actorItem.InjectProperties(_systemPropertiesDataSource);
        return actorItem;
    }

    public object? GetProperty(string propertyPath)
    {
        return _systemPropertiesDataSource.GetValue(Name, propertyPath);
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
        actorItem.Init();
    }

    public virtual void Start()
    {
        Logger.Info($"Starting Actor instance. ({Name})");
        _thread.Name = $"{Name}";
        _thread.Start();
    }

    private void RunThread()
    {
        Logger.Info($"Thread is starting. ({Name})");
        TimeManager.Register();
        ScenarioFlowTester.OnCheckpoint();
        try
        {
            while (true)
                if (
                    _mailbox.TryTake(
                        out var message,
                        MessageFetchTimeout,
                        _cancellationTokenSource.Token
                    )
                )
                {
                    if (message.Name == "_terminate")
                    {
                        _stateStack.ToList().ForEach(x => x.Dispose());
                        break;
                    }

                    MessageHandler(message);
                }
                else
                {
                    MessageHandler(new TimeoutMessage(this));
                }
        }
        catch (OperationCanceledException e)
        {
            Logger.Info("Thread is cancelled.", e);
        }
        catch (PlatformException e)
        {
            Logger.Fatal("PlatformException occured in thread.", e);
            LastPlatformException = e;
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
                var oldStateHashes = new HashSet<IState>(_stateStack);
                var result = ProcessMessage(message);
                OnMessageProcessed((message, oldState, State, result));
                if (oldState != State)
                {
                    if (!result)
                        throw new PlatformException(
                            "State has changed but ProcessMessage() returns false."
                        );
                    var newStateHashes = new HashSet<IState>(_stateStack);
                    oldStateHashes.ExceptWith(newStateHashes);
                    oldStateHashes.ToList().ForEach(x => x.Dispose());

                    StateLogger.Info($"{oldState.GetType().Name}->{State.GetType().Name}");
                    OnStateChanged((oldState, State));
                    ScenarioFlowTester.OnCheckpoint();
                    message = new StateEntryMessage(this);
                    Ui?.Send(new Message(this, "_stateChanged", State.GetType().Name));
                    continue;
                }

                if (
                    !result
                    && message.GetType() != typeof(DroppedMessage)
                    && message.GetType() != typeof(StateEntryMessage)
                )
                    message.Sender.Send(new DroppedMessage(message.Id, this));
                break;
            }
        }
        catch (SequenceError error)
        {
            var oldState = State;
            var oldStateHashes = new HashSet<IState>(_stateStack);
            if (error is FatalSequenceError fatalError)
            {
                Logger.Fatal("Fatal Sequence Error", fatalError);
                oldStateHashes.ToList().ForEach(x => x.Dispose());
                State = CreateFatalErrorState(fatalError);
            }
            else
            {
                Logger.Error("Sequence Error", error);
                oldStateHashes.ToList().ForEach(x => x.Dispose());
                State = CreateErrorState(error);
            }

            StateLogger.Info($"{oldState.GetType().Name}->{State.GetType().Name}");
            OnStateChanged((oldState, State));
            ScenarioFlowTester.OnCheckpoint();
            Ui?.Send(new Message(this, "_stateChanged", State.GetType().Name));
            MessageHandler(new StateEntryMessage(this));
        }
    }

    protected virtual IState CreateFatalErrorState(FatalSequenceError fatalError)
    {
        return _initialState;
    }

    protected virtual IState CreateErrorState(SequenceError error)
    {
        return new ErrorState(this, error);
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
            PeerStatus[peer] = new Dict();
        }
    }

    private void OnMessageProcessed(
        (Message message, IState oldState, IState newState, bool result) e
    )
    {
        MessageProcessed?.Invoke(this, e);
    }

    protected virtual void OnStateChanged((IState oldState, IState newState) e)
    {
        StateChanged?.Invoke(this, e);
    }
}
