using System;
using System.Collections.Generic;
using ControlBee.Models;
using ControlBee.TestUtils;
using ControlBeeAbstract.Exceptions;
using ControlBeeTest.TestUtils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.TestUtils;

[TestSubject(typeof(ActorUtils))]
public class ActorUtilsTest : ActorFactoryBase
{
    [Fact]
    public void SendSignalByActorTest()
    {
        var actor1 = MockActorFactory.Create("Actor1");
        var actor2 = MockActorFactory.Create("Actor2");
        ActorUtils.SendSignalByActor(actor1, actor2, "Hello");
        ActorUtils.VerifyGetSignalByActor(actor1, actor2, "Hello", Times.Once);
    }

    [Fact]
    public void EnsureAllStatusFalseTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.SetStatus("ReadyToDo", Guid.Empty);
        actor.SetStatus("Done", false);
        actor.SetStatusByActor("Peer", "ReadyToDo", Guid.Empty);
        actor.SetStatusByActor("Peer", "Done", false);
        ActorUtils.EnsureAllStatusFalse(actor);
    }

    [Fact]
    public void EnsureAllStatusFalseWithGuidTest()
    {
        var actor = ActorFactory.Create<Actor>("MyActor");
        actor.SetStatusByActor("Peer", "ReadyToDo", Guid.NewGuid());
        Assert.Throws<ValueError>(() => ActorUtils.EnsureAllStatusFalse(actor));
    }

    [Fact]
    public void SetupActionOnSignalTest()
    {
        var sendMock = new SendMock();
        var actor1 = MockActorFactory.Create("Actor1");
        var actor2 = MockActorFactory.Create("Actor2");

        var callCount = 0;
        sendMock.SetupActionOnSignal(
            actor1,
            actor2,
            "Hello",
            message =>
            {
                callCount++;
            }
        );
        ActorUtils.SendSignal(actor1, actor2, "Hello");
        ActorUtils.SendSignal(actor1, actor2, "Hello");
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void SetupActionOnSignalWithVariousTypesTest()
    {
        var sendMock = new SendMock();
        var actor1 = MockActorFactory.Create("Actor1");
        var actor2 = MockActorFactory.Create("Actor2");
        var callCount = new Dictionary<string, int>();

        var signalNames = (string[])["Hello", "World", "Request1", "Request2", "Param"];
        foreach (var signalName in signalNames)
        {
            callCount[signalName] = 0;
            sendMock.SetupActionOnSignal(
                actor1,
                actor2,
                signalName,
                message =>
                {
                    callCount[signalName]++;
                }
            );
        }

        ActorUtils.SendSignal(actor1, actor2, "Hello");
        ActorUtils.SendSignal(actor1, actor2, "World", false);
        ActorUtils.SendSignal(actor1, actor2, "Request1", Guid.NewGuid());
        ActorUtils.SendSignal(actor1, actor2, "Request2", Guid.Empty);
        ActorUtils.SendSignal(actor1, actor2, "Param", (1, 2));
        Assert.Equal(1, callCount["Hello"]);
        Assert.Equal(0, callCount["World"]);
        Assert.Equal(1, callCount["Request1"]);
        Assert.Equal(0, callCount["Request2"]);
        Assert.Equal(1, callCount["Param"]);
    }

    [Fact]
    public void SetupActionOnSignalByActorWithVariousTypesTest()
    {
        var sendMock = new SendMock();
        var actor1 = MockActorFactory.Create("Actor1");
        var actor2 = MockActorFactory.Create("Actor2");
        var callCount = new Dictionary<string, int>();

        var signalNames = (string[])["Hello", "World", "Request1", "Request2", "Param"];
        foreach (var signalName in signalNames)
        {
            callCount[signalName] = 0;
            sendMock.SetupActionOnSignalByActor(
                actor1,
                actor2,
                signalName,
                message =>
                {
                    callCount[signalName]++;
                }
            );
        }

        ActorUtils.SendSignalByActor(actor1, actor2, "Hello");
        ActorUtils.SendSignalByActor(actor1, actor2, "World", false);
        ActorUtils.SendSignalByActor(actor1, actor2, "Request1", Guid.NewGuid());
        ActorUtils.SendSignalByActor(actor1, actor2, "Request2", Guid.Empty);
        ActorUtils.SendSignalByActor(actor1, actor2, "Param", (1, 2));
        Assert.Equal(1, callCount["Hello"]);
        Assert.Equal(0, callCount["World"]);
        Assert.Equal(1, callCount["Request1"]);
        Assert.Equal(0, callCount["Request2"]);
        Assert.Equal(1, callCount["Param"]);
    }
}
