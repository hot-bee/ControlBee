using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using Moq;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.TestUtils;

public class ActorUtils
{
    public static void EnsureAllStatusFalse(Actor actor)
    {
        EnsureAllStatusFalse(actor.Status);
    }

    public static void EnsureAllStatusFalse(Dict dict)
    {
        foreach (var (key, value) in dict)
        {
            if (key == "_error")
                continue;
            if (value is true)
                throw new Exception();
            if (value is Dict nested)
                EnsureAllStatusFalse(nested);
        }
    }

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
            .Callback(action);
    }

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
            .Callback(action);
    }

    public static void SetupSignalByActor(
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
            });
    }

    public static void SetupErrorSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        IActor actorInError
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
                actorFrom.Send(
                    new Message(actorInError, "_status", new Dict { ["_error"] = true })
                );
            });
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

    public static void VerifyGetSignalByActor(IActor actorTo, string signalName, Func<Times> times)
    {
        Mock.Get(actorTo)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                                as bool?
                                == true
                        )
                    ),
                times
            );
    }

    public static void SetupActionOnGetMessage(
        IActor actor,
        string messageName,
        Action<Message> action
    )
    {
        Mock.Get(actor)
            .Setup(m => m.Send(It.Is<Message>(message => message.Name == messageName)))
            .Callback(action);
    }

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

    public static void SetupReplyErrorMessage(
        IActor actorTo,
        string messageReqName,
        IActor actorInError
    )
    {
        Mock.Get(actorTo)
            .Setup(m => m.Send(It.Is<Message>(message => message.Name == messageReqName)))
            .Callback<Message>(message =>
            {
                SendErrorSignal(actorInError, message.Sender);
            })
            .Returns<Message>(message => message.Id);
    }

    public static void VerifyGetMessage(IActor actorTo, string messageName, Func<Times> times)
    {
        Mock.Get(actorTo)
            .Verify(m => m.Send(It.Is<Message>(message => message.Name == messageName)), times);
    }

    public static void VerifyGetMessage(
        Guid requestId,
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
                            message.RequestId == requestId && message.Name == messageName
                        )
                    ),
                times
            );
    }

    public static void VerifyGetMessage(
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
                            message.Name == messageName && payload.Equals(message.Payload)
                        )
                    ),
                times
            );
    }
}
