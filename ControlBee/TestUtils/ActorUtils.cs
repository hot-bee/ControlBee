using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using ControlBeeAbstract.Exceptions;
using Moq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBeeTest.TestUtils;

public class ActorUtils
{
    public static void EnsureAllStatusFalse(Actor actor, Dict? excludes = null)
    {
        EnsureAllStatusFalse(actor.Status, excludes);
    }

    public static void EnsureAllStatusFalse(Dict dict, Dict? excludes)
    {
        foreach (var (key, value) in dict)
        {
            if (key == "_error")
                continue;
            if (excludes?.ContainsKey(key) is true)
                continue;
            if (value is true)
                throw new ValueError("Boolean value is true.");
            if (value is Guid guid && guid != Guid.Empty)
                throw new ValueError("GUID is not empty.");
            if (value is Dict nested)
                EnsureAllStatusFalse(nested, excludes?.GetValueOrDefault(key) as Dict);
        }
    }

    [Obsolete]
    public static void SetupActionOnRequestByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        Action<Message> action
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                            is Guid
                    )
                )
            )
            .Callback(action)
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupActionOnSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        Action action
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                            as bool?
                            == true
                    )
                )
            )
            .Callback(action)
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupActionOnSignal(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        Action action
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && message.Sender == actorFrom
                        && DictPath.Start(message.DictPayload)[signalName].Value as bool? == true
                    )
                )
            )
            .Callback(action)
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupReplySignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalNameFrom,
        string signalNameTo
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorTo.Name][signalNameFrom].Value
                            as bool?
                            == true
                    )
                )
            )
            .Callback(() =>
            {
                actorFrom.Send(
                    new Message(
                        actorTo,
                        "_status",
                        new Dict { [actorFrom.Name] = new Dict { [signalNameTo] = true } }
                    )
                );
            })
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupReplyErrorSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                            as bool?
                            == true
                    )
                )
            )
            .Callback(() =>
            {
                actorFrom.Send(new Message(actorTo, "_status", new Dict { ["_error"] = true }));
            })
            .Returns<Message>(message => message.Id);
    }

    public static void TerminateWhenStateChanged(Actor actor, Type stateType)
    {
        actor.StateChanged += (_, tuple) =>
        {
            var (_, newState) = tuple;
            if (newState.GetType() == stateType)
                actor.Send(new TerminateMessage());
        };
    }

    public static void SetupActionOnStateChanged(Actor actor, Type stateType, Action<IState> action)
    {
        actor.StateChanged += (_, tuple) =>
        {
            var (_, newState) = tuple;
            if (newState.GetType() == stateType)
                action(newState);
        };
    }

    public static void SendSignalByActor(IActor actorFrom, IActor actorTo, string signalName)
    {
        SendSignalByActor(actorFrom, actorTo, signalName, true);
    }

    public static void SendSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        object? signalValue
    )
    {
        actorTo.Send(
            new Message(
                actorFrom,
                "_status",
                new Dict { [actorTo.Name] = new Dict { [signalName] = signalValue } }
            )
        );
    }

    public static void SendSignalByActor(IActor actorFrom, IActor actorTo, Dict signals)
    {
        actorTo.Send(new Message(actorFrom, "_status", new Dict { [actorTo.Name] = signals }));
    }

    public static void SendSignal(IActor actorFrom, IActor actorTo, string signalName)
    {
        SendSignal(actorFrom, actorTo, signalName, true);
    }

    public static void SendSignal(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        object? signalValue
    )
    {
        actorTo.Send(new Message(actorFrom, "_status", new Dict { [signalName] = signalValue }));
    }

    public static void SendErrorSignal(IActor actorFrom, IActor actorTo)
    {
        actorTo.Send(new Message(actorFrom, "_status", new Dict { ["_error"] = true }));
    }

    public static void VerifyGetSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        Func<Times> times
    )
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && message.Sender == actorFrom
                            && DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                                as bool?
                                == true
                        )
                    ),
                times
            );
    }

    [Obsolete]
    public static void SetupActionOnGetMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        Action<Message> action
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Sender == actorFrom && message.Name == messageName
                    )
                )
            )
            .Callback(action)
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupFuncOnGetMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        Func<Message, Guid> func
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Sender == actorFrom && message.Name == messageName
                    )
                )
            )
            .Returns(func);
    }

    [Obsolete]
    public static void SetupReplyMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageReqName,
        string messageResName
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Sender == actorFrom && message.Name == messageReqName
                    )
                )
            )
            .Callback<Message>(message =>
            {
                message.Sender.Send(new Message(message, actorTo, messageResName));
            })
            .Returns<Message>(message => message.Id);
    }

    [Obsolete]
    public static void SetupReplyErrorMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageReqName
    )
    {
        Mock.Get(actorTo)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Sender == actorFrom && message.Name == messageReqName
                    )
                )
            )
            .Callback<Message>(message =>
            {
                SendErrorSignal(actorTo, message.Sender);
            })
            .Returns<Message>(message => message.Id);
    }

    public static void VerifyGetMessage(IActor actorTo, string messageName, Func<Times> times)
    {
        Mock.Get(actorTo)
            .Verify(m => m.Send(It.Is<Message>(message => message.Name == messageName)), times);
    }

    public static void VerifyGetMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        Func<Times> times
    )
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Sender == actorFrom && message.Name == messageName
                        )
                    ),
                times
            );
    }

    public static void VerifyGetMessage(
        Guid requestId,
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        Func<Times> times
    )
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Sender == actorFrom
                            && message.RequestId == requestId
                            && message.Name == messageName
                        )
                    ),
                times
            );
    }

    public static void VerifyGetMessage(
        Guid requestId,
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        object payload,
        Func<Times> times
    )
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Sender == actorFrom
                            && message.RequestId == requestId
                            && message.Name == messageName
                            && payload.Equals(message.Payload)
                        )
                    ),
                times
            );
    }

    public static void VerifyGetMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        object payload,
        Func<Times> times
    )
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Sender == actorFrom
                            && message.Name == messageName
                            && payload.Equals(message.Payload)
                        )
                    ),
                times
            );
    }
}
