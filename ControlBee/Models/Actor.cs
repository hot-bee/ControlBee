using System.Collections.Concurrent;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class Actor : IActor, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Action<IActor, Message>? _messageHandler;
    private readonly Thread _thread;

    public Actor() : this(new ActorConfig(string.Empty))
    {
    }

    public Actor(Action<IActor, Message> messageHandler)
        : this()
    {
        _messageHandler = messageHandler;
    }

    public Actor(ActorConfig config)
    {
        Logger.Info("Creating an instance of Actor.");
        _thread = new Thread(RunThread);
        VariableManager = config.VariableManager;
        ActorName = config.ActorName;
    }

    public IVariableManager? VariableManager { get; }
    public string ActorName { get; }

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
    }

    public void Join()
    {
        _thread.Join();
    }
}