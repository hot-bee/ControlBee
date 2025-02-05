using ControlBee.Interfaces;

namespace ControlBee.Models;

public class Message(IActor sender, string name, object? payload)
{
    public static readonly Message Empty = new(EmptyActor.Instance, "_empty");

    public Message(IActor sender, string name)
        : this(sender, name, null) { }

    public Message(Guid requestId, IActor sender, string name, object? payload)
        : this(sender, name, payload)
    {
        RequestId = requestId;
    }

    public Message(Guid requestId, IActor sender, string name)
        : this(requestId, sender, name, null) { }

    public Message(Message requestMessage, IActor sender, string name, object? payload)
        : this(requestMessage.Id, sender, name, payload) { }

    public Message(Message requestMessage, IActor sender, string name)
        : this(requestMessage, sender, name, null) { }

    public string ActorName => Sender.Name;

    public Guid Id { get; } = Guid.NewGuid();
    public Guid RequestId { get; } = Guid.Empty;

    public IActor Sender { get; } = sender;
    public string Name { get; } = name;
    public object? Payload { get; } = payload;
    public Dictionary<string, object?>? DictPayload { get; } =
        payload as Dictionary<string, object?>;
}
