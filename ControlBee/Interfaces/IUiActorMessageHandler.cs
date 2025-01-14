using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IUiActorMessageHandler
{
    void ProcessMessage(Message message);
    void SetCallback(Action<Message> publishMessage);
}
