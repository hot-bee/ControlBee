using ControlBee.Interfaces;

namespace ControlBee.Models;

public class UiActor(ActorConfig config) : Actor(config), IUiActor
{
    private IUiActorMessageHandler? _messageHandler;

    public event EventHandler<Message>? MessageArrived;

    public override Guid Send(Message message)
    {
        switch (message.Name)
        {
            case "_status":
            {
                lock (PeerStatus)
                {
                    if (!PeerStatus.TryGetValue(message.Sender, out var peerStatus))
                    {
                        peerStatus = [];
                        PeerStatus[message.Sender] = peerStatus;
                    }

                    foreach (var (key, value) in message.DictPayload!)
                        peerStatus[key] = value;
                }
                break;
            }
        }
        _messageHandler?.ProcessMessage(message);
        return message.Id;
    }

    public override void Start()
    {
        // Do nothing
    }

    public void SetHandler(IUiActorMessageHandler handler)
    {
        _messageHandler = handler;
        _messageHandler.SetCallback(PublishMessage);
    }

    private void PublishMessage(Message message)
    {
        OnMessageArrived(message);
    }

    protected virtual void OnMessageArrived(Message e)
    {
        MessageArrived?.Invoke(this, e);
    }

    public override object? GetPeerStatus(IActor actor, string keyName)
    {
        lock (PeerStatus)
        {
            return PeerStatus[actor].GetValueOrDefault(keyName);
        }
    }
}
