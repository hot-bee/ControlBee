using ControlBee.Interfaces;

namespace ControlBee.Models;

public class UiActor(ActorConfig config) : Actor(config), IUiActor
{
    private IUiActorMessageHandler? _messageHandler;

    public event EventHandler<Message>? MessageArrived;

    public override void Start()
    {
        // Do nothing
    }

    public override Guid Send(Message message)
    {
        _messageHandler?.ProcessMessage(message);
        return message.Id;
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
}
