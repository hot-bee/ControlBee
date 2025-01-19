using System.Collections.Concurrent;
using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBee.Variables;
using log4net;

namespace ControlBee.Models;

public class Actor : IActorInternal, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    public static readonly Actor Empty = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Func<IActor, IState, Message, IState>? _messageHandler;
    private readonly Thread _thread;

    private bool _init;

    private string _title = string.Empty;

    protected IActor InternalUi = Empty;

    public IState State;

    public Actor()
        : this(
            new ActorConfig(
                string.Empty,
                new EmptyAxisFactory(),
                new EmptyVariableManager(),
                new TimeManager()
            )
        ) { }

    public Actor(string actorName)
        : this(
            new ActorConfig(
                actorName,
                new EmptyAxisFactory(),
                new EmptyVariableManager(),
                new TimeManager()
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
        VariableManager = config.VariableManager;
        TimeManager = config.TimeManager;
        PositionAxesMap = new PositionAxesMap();
        Name = config.ActorName;

        State = new EmptyState(this);
    }

    public IAxisFactory AxisFactory { get; }

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

    public virtual void Send(Message message)
    {
        _mailbox.Add(message);
    }

    public ITimeManager TimeManager { get; }
    public IActor Ui => InternalUi;

    public IPositionAxesMap PositionAxesMap { get; }
    public IVariableManager VariableManager { get; }

    public void Init()
    {
        if (_init)
            throw new ApplicationException();
        _init = true;

        InitActorItems(string.Empty, this);
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

    private void InitActorItems(string itemNamePrefix, object actorItemHolder)
    {
        var fieldInfos = actorItemHolder.GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IActorItem)))
            {
                var actorItem = (IActorItem)fieldInfo.GetValue(actorItemHolder)!;
                var itemName = string.Join('/', itemNamePrefix, fieldInfo.Name);
                actorItem.Actor = this;
                actorItem.ItemName = itemName;
                actorItem.UpdateSubItem();

                if (actorItem is IVariable variable)
                    VariableManager?.Add(variable);

                InitActorItems(itemName, actorItem);
            }
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

    protected virtual void ProcessMessage(Message message)
    {
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

    protected virtual void OnMessageProcessed((Message message, IState oldState, IState newState) e)
    {
        MessageProcessed?.Invoke(this, e);
    }
}
