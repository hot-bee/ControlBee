namespace ControlBee;

public class Message(IActor sender, object payload)
{
    public IActor Sender { get; } = sender;
    public object Payload { get; } = payload;
}
