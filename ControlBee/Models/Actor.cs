﻿using System.Collections.Concurrent;
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
    private readonly Thread _thread;

    private bool _init;

    private readonly Action<IActor, Message>? _messageHandler;

    protected IActor InternalUi = Empty;

    protected IState State;

    public Actor()
        : this(new ActorConfig(string.Empty, new EmptyVariableManager(), new TimeManager())) { }

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
        TimeManager = config.TimeManager;
        PositionAxesMap = new PositionAxesMap();
        ActorName = config.ActorName;
        State = new EmptyState(this);
    }

    public string ActorName { get; }

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
        _cancellationTokenSource.Cancel();
        _thread.Join();
        Logger.Info("Actor instance successfully disposed.");
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
