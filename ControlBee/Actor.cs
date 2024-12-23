﻿using System.Collections.Concurrent;
using log4net;

namespace ControlBee;

public class Actor : IActor, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Actor));
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Action<IActor, Message>? _processHandler;
    private readonly Thread _thread;

    public Actor()
    {
        Logger.Info("Instantiating Actor.");
        _thread = new Thread(RunThread);
    }

    public Actor(Action<IActor, Message> processHandler)
        : this()
    {
        _processHandler = processHandler;
    }

    public void Send(Message message)
    {
        _mailbox.Add(message);
    }

    public void Dispose()
    {
        Logger.Info("Disposing Actor.");
        _cancellationTokenSource.Cancel();
        _thread.Join();
        Logger.Info("Disposed Actor.");
    }

    public void Start()
    {
        Logger.Info("Starting Actor.");
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
        _processHandler?.Invoke(this, message);
    }

    public void Join()
    {
        _thread.Join();
    }
}
