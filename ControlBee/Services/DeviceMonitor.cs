﻿using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Services;

public class DeviceMonitor : IDeviceMonitor
{
    private static readonly ILog Logger = LogManager.GetLogger("General");
    private readonly Dictionary<(string actorName, string itemPath), bool> _alwaysUpdates = [];

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Dictionary<(string actorName, string itemPath), IDeviceChannel> _channelMap = [];
    private readonly List<IDeviceChannel> _channels = [];
    private readonly Thread _thread;

    public DeviceMonitor()
    {
        _thread = new Thread(Monitoring)
        {
            Name = nameof(DeviceMonitor),
            Priority = ThreadPriority.BelowNormal
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

    public void SetAlwaysUpdate(string actorName, string itemPath)
    {
        _alwaysUpdates[(actorName, itemPath)] = true;
    }

    public bool GetAlwaysUpdate(string actorName, string itemPath)
    {
        return _alwaysUpdates.GetValueOrDefault((actorName, itemPath), false);
    }

    public void Start()
    {
        if (_thread.ThreadState != ThreadState.Unstarted)
        {
            Logger.Error("Thread is already started.");
            return;
        }

        Init();
        _thread.Start();
    }

    public void Init()
    {
        foreach (var channel in _channels)
        {
            var actorName = channel.Actor.Name;
            var itemPath = channel.ItemPath;
            _channelMap.Add((actorName, itemPath), channel);
        }
    }

    private void Monitoring()
    {
        var token = _cancellationTokenSource.Token;
        try
        {
            while (true)
                foreach (var (key, channel)in _channelMap)
                {
                    token.ThrowIfCancellationRequested();
                    var alwaysUpdate = GetAlwaysUpdate(key.actorName, key.itemPath);
                    channel.RefreshCache(alwaysUpdate);
                    Thread.Sleep(1);
                }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Exit Monitoring.");
        }
    }
}