using System.Collections.Concurrent;

namespace ControlBee;

public class Actor : IActor, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<Message> _mailbox = new();

    private readonly Action<IActor, Message>? _processHandler;
    private readonly Thread _thread;

    public Actor()
    {
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
        _cancellationTokenSource.Cancel();
        _thread.Join();
    }

    public void Start()
    {
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
        catch (OperationCanceledException)
        {
            // TODO: Log here.
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
