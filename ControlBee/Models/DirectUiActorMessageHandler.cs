using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DirectUiActorMessageHandler : IUiActorMessageHandler
{
    private Action<Message>? _publishMessage;

    public void ProcessMessage(Message message)
    {
        _publishMessage!(message);
    }

    public void SetCallback(Action<Message> publishMessage)
    {
        _publishMessage = publishMessage;
    }
}
