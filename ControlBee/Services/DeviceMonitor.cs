using System.Reflection;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Services;

public class DeviceMonitor : IDisposable, IDeviceMonitor
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<IDeviceChannel> _channels = [];
    private readonly Thread _thread;

    public DeviceMonitor()
    {
        _thread = new Thread(Monitoring)
        {
            Name = nameof(DeviceMonitor),
            Priority = ThreadPriority.BelowNormal,
        };
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _thread.Join();
    }

    public void Add(IDeviceChannel channel)
    {
        _channels.Add(channel);
    }

    private void Monitoring()
    {
        var token = _cancellationTokenSource.Token;
        try
        {
            while (true)
                foreach (var channel in _channels)
                {
                    token.ThrowIfCancellationRequested();
                    channel.RefreshCache();
                    Thread.Sleep(1);
                }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Exit Monitoring.");
        }
    }

    public void Start()
    {
        if (_thread.ThreadState != ThreadState.Unstarted)
        {
            Logger.Error("Thread is already started.");
            return;
        }

        _thread.Start();
    }
}
