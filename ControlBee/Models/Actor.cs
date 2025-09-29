using System.Collections.Concurrent;
using System.Reflection;
using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBee.Utils;
using ControlBeeAbstract.Exceptions;
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

    private int _publishStep;

    private string _title = string.Empty;
    public IDialog CrashError = new DialogPlaceholder();

    public PlatformException? ExitError;

    public IDialog FatalError = new DialogPlaceholder();

    public Dictionary<string, IActor> PeerDict = [];
    public Dictionary<IActor, Dict> PeerStatus = new();

    public Dict Status = new();

    public Actor(ActorConfig config)
    {
        Logger.Info($"Creating an instance of Actor. ({config.ActorName})");
        _thread = new Thread(RunThread);

        SkipWaitSensor = config.SystemConfigurations.SkipWaitSensor;
        VariableManager = config.VariableManager;
        DeviceManager = config.DeviceManager;
        TimeManager = config.TimeManager;
        ScenarioFlowTester = config.ScenarioFlowTester;
        _systemPropertiesDataSource = config.SystemPropertiesDataSource;
        EventManager = config.EventManager;
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
            {
                Logger.Warn(
                    "State Stack Count is greater than 1, but a new state is being set after clearing all existing states"
                );
                Logger.Warn(LoggerUtils.CurrentStackDefaultLog());
            }

            _stateStack.Clear();
            _stateStack.Push(value);
        }
    }

    public int StateStackCount => _stateStack.Count;

    public bool SkipWaitSensor { get; }

    public int MessageFetchTimeout { get; set; } = -1;

    public IScenarioFlowTester ScenarioFlowTester { get; }
    public IEventManager EventManager { get; }
    public IDeviceManager DeviceManager { get; }

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
            MessageLogger.Debug(content);
        return message.Id;
    }

    public (string itemPath, Type type)[] GetItems()
    {
        return _actorItems.Where(x => x.Value.Visible)
            .ToList().ConvertAll(x => (x.Key, x.Value.GetType())).ToArray();
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
        IterateItems(string.Empty, this, PostInitItem, config);
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
        return GetRegisteredFunctions().Where(IsFunctionAvailable).ToArray();
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

    protected virtual string[] GetRegisteredFunctions()
    {
        return [];
    }

    protected virtual bool IsFunctionAvailable(string functionName)
    {
        return true;
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

    public void PublishStepIn()
    {
        _publishStep++;
    }

    public void PublishStepOut()
    {
        _publishStep--;
        if (_publishStep == 0)
            PublishStatus();
    }

    public void PublishStatus()
    {
        if (_publishStep > 0)
            return;
        var clonedStatus = DictCopy.Copy(Status);
        foreach (var peer in PeerDict.Values)
            peer.Send(new Message(this, "_status", clonedStatus));
    }

    public void SetStatus(string name, object? value)
    {
        if (value != null && Status.TryGetValue(name, out var oldValue))
            if (value.Equals(oldValue))
                return;
        StatusLogger.Debug($"SetStatus: {name}, {value}");
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
        StatusLogger.Debug($"SetStatusByActor: {actorName}, {keyName}, {value}");
        statusByActor[keyName] = value;
        Status[actorName] = statusByActor;
        PublishStatus();
    }

    public void SetStatusByActor(IActor? actor, string keyName, object? value)
    {
        if (actor == null) return;
        SetStatusByActor(actor.Name, keyName, value);
    }

    public object? GetStatusByActor(string actorName, string keyName)
    {
        var statusByActor = Status.GetValueOrDefault(actorName) as Dict ?? new Dict();
        statusByActor.TryGetValue(keyName, out var value);
        return value;
    }

    public object? GetStatusByActor(IActor actor, string keyName)
    {
        return GetStatusByActor(actor.Name, keyName);
    }

    public object? GetPeerStatus(IActor actor, string keyName)
    {
        return PeerStatus[actor].GetValueOrDefault(keyName);
    }

    public object? GetPeerStatus(string actorName, string keyName)
    {
        return GetPeerStatus(PeerDict[actorName], keyName);
    }

    public object? GetPeerStatusByActor(IActor? actor, string keyName)
    {
        if (actor == null) return null;
        return (GetPeerStatus(actor, Name) as Dict)?.GetValueOrDefault(keyName);
    }

    public object? GetPeerStatusByActor(string actorName, string keyName)
    {
        return GetPeerStatusByActor(PeerDict[actorName], keyName);
    }

    public bool HasPeerError(IActor? peer)
    {
        if (peer == null) return false;
        return GetPeerStatus(peer, "_error") is true;
    }

    public bool IsPeerInactive(IActor? peer)
    {
        if (peer == null) return false;
        return GetPeerStatus(peer, "_inactive") is true;
    }

    public bool HasPeerFailed(IActor? peer)
    {
        return HasPeerError(peer) || IsPeerInactive(peer);
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
        Func<object, IActorItem, Type, FieldInfo, int, string, ActorConfig, IActorItem> func,
        ActorConfig config
    )
    {
        var fieldInfos = actorItemHolder.GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
        {
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IActorItem)))
            {
                var itemPath = string.Join('/', itemPathPrefix, fieldInfo.Name);
                if (fieldInfo.GetValue(actorItemHolder)! is not IActorItem actorItem) continue;
                actorItem = func(actorItemHolder, actorItem, fieldInfo.FieldType, fieldInfo, -1, itemPath, config);

                IterateItems(itemPath, actorItem, func, config);
            }

            if (fieldInfo.FieldType.IsAssignableTo(typeof(IActorItem[])))
            {
                var array = (IActorItem[])fieldInfo.GetValue(this)!;
                for (var i = 0; i < array.Length; i++)
                {
                    var itemPath = string.Join('/', itemPathPrefix, fieldInfo.Name, $"{i}");
                    if (array[i] is not { } actorItem) continue;
                    actorItem = func(actorItemHolder, actorItem, array[i].GetType(), fieldInfo, i, itemPath, config);

                    IterateItems(itemPath, actorItem, func, config);
                }
            }
        }
    }

    private IActorItem InitItem(
        object actorItemHolder,
        IActorItem actorItem,
        Type type,
        FieldInfo fieldInfo,
        int index,
        string itemPath,
        ActorConfig config
    )
    {
        if (actorItem is IPlaceholder placeHolder)
        {
            IActorItem newItem;
            if (type.IsAssignableTo(typeof(IDigitalInput)))
                newItem = config.DigitalInputFactory.Create();
            else if (type.IsAssignableTo(typeof(IDigitalOutput)))
                newItem = config.DigitalOutputFactory.Create();
            else if (type.IsAssignableTo(typeof(IAnalogInput)))
                newItem = config.AnalogInputFactory.Create();
            else if (type.IsAssignableTo(typeof(IAnalogOutput)))
                newItem = config.AnalogOutputFactory.Create();
            else if (type.IsAssignableTo(typeof(IDialog)))
                newItem = config.DialogFactory.Create();
            else if (type.IsAssignableTo(typeof(IVision)))
                newItem = config.VisionFactory.Create();
            else
                throw new ValueError();
            _placeholderManager.Add(placeHolder, newItem);
            actorItem = newItem;
            if (index == -1)
            {
                fieldInfo.SetValue(actorItemHolder, actorItem);
            }
            else
            {
                var array = (IActorItem[])fieldInfo.GetValue(actorItemHolder)!;
                array[index] = actorItem;
            }
        }

        AddItem(actorItem, itemPath);
        actorItem.InjectProperties(_systemPropertiesDataSource);
        return actorItem;
    }

    private IActorItem PostInitItem(
        object actorItemHolder,
        IActorItem actorItem,
        Type type,
        FieldInfo fieldInfo,
        int index,
        string itemPath,
        ActorConfig config
    )
    {
        actorItem.PostInit();
        return actorItem;
    }

    public object? GetProperty(string propertyPath)
    {
        return _systemPropertiesDataSource.GetValue(Name, propertyPath);
    }

    private IActorItem ReplacePlaceholder(
        object actorItemHolder,
        IActorItem actorItem,
        Type type,
        FieldInfo fieldInfo,
        int index,
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
        PublishStateChanged();
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
            ExitError = e;
            CrashError.Show(e.Message);
        }
        finally
        {
            TimeManager.Unregister();
        }
    }

    private void PublishStateChanged()
    {
        Ui?.Send(new Message(this, "_stateChanged", State.GetType().Name));
    }

    protected virtual bool ProcessMessage(Message message)
    {
        var result = ActorBuiltinMessageHandler.ProcessMessage(message);
        result |= State.ProcessMessage(message);
        if (message is ActorItemMessage actorItemMessage)
        {
            var item = GetItem(actorItemMessage.ItemPath);
            result |= item?.ProcessMessage(actorItemMessage) ?? false;
        }

        return result;
    }

    protected virtual void MessageHandler(Message message)
    {
        try
        {
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

                    StateLogger.Debug($"{oldState.GetType().Name}->{State.GetType().Name}");
                    OnStateChanged((oldState, State));
                    ScenarioFlowTester.OnCheckpoint();
                    message = new StateEntryMessage(this);
                    PublishStateChanged();
                    continue;
                }

                if (
                    !result
                    && message.GetType() != typeof(DroppedMessage)
                    && message.GetType() != typeof(StateEntryMessage)
                    && message.GetType() != typeof(TimeoutMessage)
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
                FatalError.Show(fatalError.Message);
            }
            else
            {
                Logger.Error("Sequence Error", error);
                oldStateHashes.ToList().ForEach(x => x.Dispose());
                State = CreateErrorState(error);
            }

            StateLogger.Debug($"{oldState.GetType().Name}->{State.GetType().Name}");
            OnStateChanged((oldState, State));
            ScenarioFlowTester.OnCheckpoint();
            PublishStateChanged();
            MessageHandler(new StateEntryMessage(this));
        }
    }

    public virtual IState CreateIdleState()
    {
        throw new UnimplementedByDesignError("This method must be implemented in a subclass.");
    }

    protected virtual IState CreateFatalErrorState(FatalSequenceError fatalError)
    {
        return _initialState;
    }

    protected virtual IState CreateErrorState(SequenceError error)
    {
        throw new UnimplementedByDesignError("This method must be implemented in a subclass.");
    }

    public void Join()
    {
        _thread.Join();
    }

    public void InitPeers(IActor[] peerList)
    {
        var peers = peerList.ToHashSet();
        peers.Add(this);
        if (Ui != null) peers.Add(Ui);
        foreach (var peer in peers)
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