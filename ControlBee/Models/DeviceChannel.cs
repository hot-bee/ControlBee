using System.ComponentModel;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;
using log4net;

namespace ControlBee.Models;

public abstract class DeviceChannel(IDeviceManager deviceManager)
    : ActorItem,
        IDeviceChannel,
        IDeviceChannelModifier
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private static readonly Dictionary<string, DeviceMetaInfo> DeviceMetaInfoMap = [];
    private readonly DeviceMetaInfo _localDeviceMetaInfo = new();
    private IDevice? _device;

    protected IDevice? Device
    {
        get => _device;
        set
        {
            _device = value;
            if (_device != null)
                GetDeviceMetaInfo().PropertyChanged += OnDeviceMetaInfoChanged;
        }
    }

    protected string? DeviceName { get; set; }
    protected int Channel { get; set; } = -1;

    public virtual void RefreshCache(bool alwaysUpdate = false)
    {
        // Implement this on override functions
    }

    public IDevice? GetDevice()
    {
        return Device;
    }

    public int GetChannel()
    {
        return Channel;
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "_itemMetaDataRead":
                SendMetaData(message.Id);
                return true;
        }

        return base.ProcessMessage(message);
    }

    protected override void SendMetaData(Guid requestId = default)
    {
        if (Actor.Ui == null)
            return;
        var payload = new Dictionary<string, object?>
        {
            [nameof(Name)] = Name,
            [nameof(Desc)] = Desc,
            [nameof(Channel)] = Channel,
        };
        Actor.Ui.Send(
            new ActorItemMessage(requestId, Actor, ItemPath, "_itemMetaDataChanged", payload)
        );
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        if (dataSource.GetValue(ActorName, ItemPath, nameof(DeviceName)) is string deviceName)
            DeviceName = deviceName;
        if (dataSource.GetValue(ActorName, ItemPath, nameof(Channel)) is string channelIdValue)
            if (int.TryParse(channelIdValue, out var channel))
                Channel = channel;

        if (string.IsNullOrEmpty(DeviceName))
        {
            Logger.Warn($"DeviceName is empty. ({ActorName}, {ItemPath})");
            return;
        }

        if (Channel == -1)
        {
            Logger.Warn($"Channel is empty. ({ActorName}, {ItemPath})");
            return;
        }

        Device = deviceManager.Get(DeviceName!);
    }

    public void SetChannel(int channel)
    {
        Channel = channel;
    }

    public void SetDevice(string deviceName)
    {
        DeviceName = deviceName;
        Device = deviceManager.Get(DeviceName!);
    }

    private void OnDeviceMetaInfoChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DeviceMetaInfo.Aborted))
            return;
        if (GetDeviceMetaInfo().Aborted)
            OnDeviceAborted();
    }

    protected virtual void OnDeviceAborted() { }

    protected DeviceMetaInfo GetDeviceMetaInfo()
    {
        if (DeviceName == null)
            return _localDeviceMetaInfo;
        return deviceManager.GetDeviceMetaInfo(DeviceName);
    }

    public bool IsAborted()
    {
        return GetDeviceMetaInfo().Aborted;
    }

    public virtual void AbortDevice()
    {
        Logger.Info($"Abort device. ({ActorName}, {ItemPath}, {Channel})");
        GetDeviceMetaInfo().Aborted = true;
    }

    public void ResetAbort()
    {
        if (!GetDeviceMetaInfo().Aborted)
            return;
        Logger.Info($"Reset device abort. ({ActorName}, {ItemPath}, {Channel})");
        GetDeviceMetaInfo().Aborted = false;
    }

    public virtual void Sync()
    {
        // Empty
    }
}
