using System;
using System.Collections.Generic;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using Moq;

namespace ControlBee.Tests.TestUtils;

public class SendMock
{
    private readonly HashSet<IActor> _actorsSetup = [];

    private readonly Dictionary<
        (IActor actorFrom, IActor actorTo, string messageName),
        Action<Message>
    > _messageMap = [];

    private readonly Dictionary<
        (IActor actorFrom, IActor actorTo, string signalName),
        Action<Message>
    > _signalMap = [];

    public void SetupActionOnMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageName,
        Action<Message> action
    )
    {
        Setup(actorTo);
        if (!_messageMap.ContainsKey((actorFrom, actorTo, messageName)))
            _messageMap[(actorFrom, actorTo, messageName)] = _ => { };
        _messageMap[(actorFrom, actorTo, messageName)] += action;
    }

    public void RemoveActionOnSignalByActor(IActor actorFrom, IActor actorTo, string signalName)
    {
        _signalMap.Remove((actorFrom, actorTo, signalName));
    }

    public void SetupActionOnSignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalName,
        Action<Message> action
    )
    {
        Setup(actorTo);

        if (!_signalMap.ContainsKey((actorFrom, actorTo, signalName)))
            _signalMap[(actorFrom, actorTo, signalName)] = _ => { };
        _signalMap[(actorFrom, actorTo, signalName)] += action;
    }

    private void Setup(IActor actor)
    {
        if (!_actorsSetup.Add(actor))
            return;
        Mock.Get(actor)
            .Setup(m => m.Send(It.IsAny<Message>()))
            .Callback<Message>(message =>
            {
                if (message.Name == "_status")
                    foreach (var ((actorFrom, actorTo, signalName), value) in _signalMap)
                    {
                        if (message.Sender != actorFrom)
                            continue;
                        if (actor != actorTo)
                            continue;
                        if (
                            DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                            is Guid
                        )
                        {
                            value(message);
                            RemoveActionOnSignalByActor(actorFrom, actorTo, signalName);
                        }
                        if (
                            DictPath.Start(message.DictPayload)[actorTo.Name][signalName].Value
                            is true
                        )
                        {
                            value(message);
                            RemoveActionOnSignalByActor(actorFrom, actorTo, signalName);
                        }
                    }

                foreach (var ((actorFrom, actorTo, messageName), value) in _messageMap)
                {
                    if (message.Sender != actorFrom)
                        continue;
                    if (actor != actorTo)
                        continue;
                    if (message.Name == messageName)
                        value(message);
                }
            })
            .Returns<Message>(message => message.Id);
    }

    public void SetupReplySignalByActor(
        IActor actorFrom,
        IActor actorTo,
        string signalNameFrom,
        string signalNameTo
    )
    {
        SetupActionOnSignalByActor(
            actorFrom,
            actorTo,
            signalNameFrom,
            message =>
            {
                ActorUtils.SendSignalByActor(actorTo, actorFrom, signalNameTo, true);
            }
        );
    }

    public void SetupReplyMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageReqName,
        string messageResName
    )
    {
        SetupReplyMessage(actorFrom, actorTo, messageReqName, messageResName, null);
    }

    public void SetupReplyMessage(
        IActor actorFrom,
        IActor actorTo,
        string messageReqName,
        string messageResName,
        object? payload
    )
    {
        SetupActionOnMessage(
            actorFrom,
            actorTo,
            messageReqName,
            message =>
            {
                actorFrom.Send(new Message(message, actorTo, messageResName, payload));
            }
        );
    }

    public void SetupReplyErrorSignalByActor(IActor actorFrom, IActor actorTo, string signalName)
    {
        SetupActionOnSignalByActor(
            actorFrom,
            actorTo,
            signalName,
            message =>
            {
                ActorUtils.SendErrorSignal(actorTo, actorFrom);
            }
        );
    }

    public void SetupReplyErrorOnMessage(IActor actorFrom, IActor actorTo, string messageReqName)
    {
        SetupActionOnMessage(
            actorFrom,
            actorTo,
            messageReqName,
            message =>
            {
                ActorUtils.SendErrorSignal(actorTo, actorFrom);
            }
        );
    }
}
