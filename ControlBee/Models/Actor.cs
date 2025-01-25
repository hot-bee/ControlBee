using System.Collections.Concurrent;
using System.Reflection;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBee.Variables;
using log4net;

namespace ControlBee.Models;

public class Actor : IActorInternal, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    private readonly IActorItemInjectionDataSource _actorItemInjectionDataSource;

    private readonly Dictionary<string, IActorItem> _actorItems = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Func<IActor, IState, Message, IState>? _messageHandler;
    private readonly PlaceholderManager _placeholderManager = new();
    private readonly Thread _thread;

    private bool _init;

    private string _title = string.Empty;

    public IState State;

    public Actor()
        : this(string.Empty) { }

    public Actor(string actorName)
        : this(
            new ActorConfig(
                actorName,
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                EmptyInitializeSequenceFactory.Instance,
                EmptyVariableManager.Instance,
                EmptyTimeManager.Instance,
                EmptyActorItemInjectionDataSource.Instance
            )
        ) { }

    public Actor(Func<IActor, IState, Message, IState> messageHandler)
        : this()
    {
        _messageHandler = messageHandler;
    }

    public Actor(ActorConfig config)
    {
        Logger.Info($"Creating an instance of Actor. ({config.ActorName})");
        _thread = new Thread(RunThread);
        AxisFactory = config.AxisFactory;
        DigitalInputFactory = config.DigitalInputFactory;
        DigitalOutputFactory = config.DigitalOutputFactory;
        InitializeSequenceFactory = config.InitializeSequenceFactory;
        VariableManager = config.VariableManager;
        TimeManager = config.TimeManager;
        _actorItemInjectionDataSource = config.ActorItemInjectionDataSource;
        PositionAxesMap = new PositionAxesMap();
        Name = config.ActorName;
        Ui = config.UiActor;

        State = new EmptyState(this);
    }

    public IAxisFactory AxisFactory { get; } // TODO: Not here
    public IDigitalInputFactory DigitalInputFactory { get; } // TODO: Not here
    public IDigitalOutputFactory DigitalOutputFactory { get; } // TODO: Not here
    public IInitializeSequenceFactory InitializeSequenceFactory { get; }

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

    public void Init()
    {
        if (_init)
            throw new ApplicationException();
        _init = true;

        IterateItems(string.Empty, this, InitItem);
        IterateItems(string.Empty, this, ReplacePlaceholder);
        PositionAxesMap.UpdateMap();
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

    public event EventHandler<(
        Message message,
        IState oldState,
        IState newState
    )>? MessageProcessed;

    public void SetTitle(string title)
    {
        _title = title;
    }

    private void IterateItems(
        string itemPathPrefix,
        object actorItemHolder,
        Func<object, IActorItem, FieldInfo, string, IActorItem> func
    )
    {
        var fieldInfos = actorItemHolder.GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IActorItem)))
            {
                var itemPath = string.Join('/', itemPathPrefix, fieldInfo.Name);
                var actorItem = (IActorItem)fieldInfo.GetValue(actorItemHolder)!;

                actorItem = func(actorItemHolder, actorItem, fieldInfo, itemPath);

                IterateItems(itemPath, actorItem, func);
            }
    }

    private IActorItem InitItem(
        object actorItemHolder,
        IActorItem actorItem,
        FieldInfo fieldInfo,
        string itemPath
    )
    {
        if (actorItem is IPlaceholder placeHolder)
        {
            IActorItem newItem;
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IDigitalInput)))
                newItem = DigitalInputFactory.Create();
            else if (fieldInfo.FieldType.IsAssignableTo(typeof(IDigitalOutput)))
                newItem = DigitalOutputFactory.Create();
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
        string itemPath
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
            VariableManager?.Add(variable);
    }

    public virtual void Start()
    {
        Logger.Info($"Starting Actor instance. ({Name})");
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
                ProcessMessage(message);
            }
        }
        catch (OperationCanceledException e)
        {
            Logger.Info(e);
        }
        finally
        {
            TimeManager.Unregister();
        }
    }

    public IActorItem? GetItem(string itemPath)
    {
        if (!itemPath.StartsWith("/"))
            itemPath = "/" + itemPath;
        return _actorItems.GetValueOrDefault(itemPath);
    }

    protected virtual void ProcessMessage(Message message)
    {
        if (message is ActorItemMessage actorItemMessage)
        {
            var item = GetItem(actorItemMessage.ItemPath);
            item?.ProcessMessage(actorItemMessage);
            return;
        }

        while (true)
        {
            var oldState = State;
            State =
                _messageHandler != null
                    ? _messageHandler.Invoke(this, State, message)
                    : State.ProcessMessage(message);
            OnMessageProcessed((message, oldState, State));
            if (State == oldState)
                break;
            message = Message.Empty;
        }
    }

    public void Join()
    {
        _thread.Join();
    }

    private void OnMessageProcessed((Message message, IState oldState, IState newState) e)
    {
        MessageProcessed?.Invoke(this, e);
    }
}
