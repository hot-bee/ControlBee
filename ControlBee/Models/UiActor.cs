using ControlBee.Interfaces;
using ControlBee.Services;
using ControlBee.Variables;

namespace ControlBee.Models;

public class UiActor(ActorConfig config) : Actor(config), IUiActor
{
    private IUiActorMessageHandler? _messageHandler;

    public UiActor()
        : this(
            new ActorConfig(
                "ui",
                EmptyAxisFactory.Instance,
                EmptyDigitalInputFactory.Instance,
                EmptyDigitalOutputFactory.Instance,
                EmptyInitializeSequenceFactory.Instance,
                EmptyVariableManager.Instance,
                EmptyTimeManager.Instance,
                EmptyActorItemInjectionDataSource.Instance
            )
        ) { }

    public event EventHandler<Message>? MessageArrived;

    public override Guid Send(Message message)
    {
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
}
