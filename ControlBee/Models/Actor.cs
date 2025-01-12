using System.Collections.Concurrent;
using ControlBee.Interfaces;
using ControlBee.Variables;
using log4net;

namespace ControlBee.Models;

public class Actor : IActor, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    public static readonly Actor Empty = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Action<IActor, Message>? _messageHandler;
    private readonly Thread _thread;

    private bool _init;

    protected IState State;

    public Actor()
        : this(new ActorConfig(string.Empty, new EmptyVariableManager())) { }

    public Actor(Action<IActor, Message> messageHandler)
        : this()
    {
        _messageHandler = messageHandler;
    }

    public Actor(ActorConfig config)
    {
        Logger.Info($"Creating an instance of Actor. ({config.ActorName})");
        _thread = new Thread(RunThread);
        VariableManager = config.VariableManager;
        PositionAxesMap = new PositionAxesMap();
        ActorName = config.ActorName;
        State = new EmptyState(this);
    }

    public IPositionAxesMap PositionAxesMap { get; }
    public IVariableManager? VariableManager { get; }
    public string ActorName { get; }

    public void Init()
    {
        if (_init)
            throw new ApplicationException();
        _init = true;

        InitVariables();
        PositionAxesMap.UpdateMap();
    }

    public void Send(Message message)
    {
        _mailbox.Add(message);
    }

    public void Dispose()
    {
        Logger.Info("Releasing resources for Actor instance.");
        _cancellationTokenSource.Cancel();
        _thread.Join();
        Logger.Info("Actor instance successfully disposed.");
    }

    private void InitVariables()
    {
        var fieldInfos = GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
            if (fieldInfo.FieldType.IsAssignableTo(typeof(IVariable)))
            {
                var variable = (IVariable)fieldInfo.GetValue(this)!;
                variable.Actor = this;
                variable.GroupName = ActorName;
                variable.Uid = fieldInfo.Name;
                variable.UpdateSubItem();
                VariableManager?.Add(variable);
            }
    }

    public void Start()
    {
        Logger.Info("Starting Actor instance.");
        _thread.Start();
    }

    private void RunThread()
    {
        try
        {
            while (true)
            {
                var message = _mailbox.Take(_cancellationTokenSource.Token);
                if (message.Payload as string == "_terminate")
                    break;
                Process(message);
            }
        }
        catch (OperationCanceledException e)
        {
            Logger.Info(e);
        }
    }

    protected virtual void Process(Message message)
    {
        _messageHandler?.Invoke(this, message);
        while (true)
        {
            var oldState = State;
            State = State.ProcessMessage(message);
            if (State == oldState)
                break;
        }
    }

    public void Join()
    {
        _thread.Join();
    }
}
