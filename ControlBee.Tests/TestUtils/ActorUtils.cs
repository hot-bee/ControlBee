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
            if (key == "Error")
                continue;
            if (value is true)
                throw new Exception();
            if (value is Dict nested)
                EnsureAllStatusFalse(nested);
        }
    }

    public static void SetupTriggerSignal(
        IActor actorA,
        IActor actorB,
        string signalNameA,
        string signalNameB
    )
    {
        Mock.Get(actorB)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorB.Name][signalNameA].Value
                            as bool?
                            == true
                    )
                )
            )
            .Callback(() =>
            {
                actorA.Send(
                    new Message(
                        actorB,
                        "_status",
                        new Dict { [actorA.Name] = new Dict { [signalNameB] = true } }
                    )
                );
            });
    }

    public static void SetupTriggerErrorSignal(
        IActor actorA,
        IActor actorB,
        string signalNameA,
        IActor actorInError
    )
    {
        Mock.Get(actorB)
            .Setup(m =>
                m.Send(
                    It.Is<Message>(message =>
                        message.Name == "_status"
                        && DictPath.Start(message.DictPayload)[actorB.Name][signalNameA].Value
                            as bool?
                            == true
                    )
                )
            )
            .Callback(() =>
            {
                actorA.Send(new Message(actorInError, "_status", new Dict { ["Error"] = true }));
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

    public static void SendSignal(IActor actorTo, IActor actorFrom, string signalName)
    {
        actorTo.Send(
            new Message(
                actorFrom,
                "_status",
                new Dict { [actorTo.Name] = new Dict { [signalName] = true } }
            )
        );
    }

    public static void SendErrorSignal(IActor actorTo, IActor actorInError)
    {
        actorTo.Send(new Message(actorInError, "_status", new Dict { ["Error"] = true }));
    }

    public static void VerifyGetSignal(IActor actor, string signalName, Func<Times> times)
    {
        Mock.Get(actor)
            .Verify(
                m =>
                    m.Send(
                        It.Is<Message>(message =>
                            message.Name == "_status"
                            && DictPath.Start(message.DictPayload)[actor.Name][signalName].Value
                                as bool?
                                == true
                        )
                    ),
                times
            );
    }

    public static void SetupGetMessage(IActor actor, string messageName, Action<Message> action)
    {
        Mock.Get(actor)
            .Setup(m => m.Send(It.Is<Message>(message => message.Name == messageName)))
            .Callback(action);
    }

    public static void SetupReplyMessage(IActor actor, string messageReqName, string messageResName)
    {
        Mock.Get(actor)
            .Setup(m => m.Send(It.Is<Message>(message => message.Name == messageReqName)))
            .Callback<Message>(message =>
            {
                message.Sender.Send(new Message(message, actor, messageResName));
            });
    }

    public static void SetupReplyErrorMessage(
        IActor actor,
        string messageReqName,
        IActor actorInError
    )
    {
        Mock.Get(actor)
            .Setup(m => m.Send(It.Is<Message>(message => message.Name == messageReqName)))
            .Callback<Message>(message =>
            {
                SendErrorSignal(message.Sender, actorInError);
            });
    }

    public static void VerifyGetMessage(IActor actor, string messageName, Func<Times> times)
    {
        Mock.Get(actor)
            .Verify(m => m.Send(It.Is<Message>(message => message.Name == messageName)), times);
    }

    public static void VerifyGetMessage(
        IActor actor,
        string messageName,
        object payload,
        Func<Times> times
    )
    {
        Mock.Get(actor)
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
