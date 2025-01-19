using System.Collections.Generic;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Sequences;

[TestSubject(typeof(GlobalInitializationSequence))]
public class GlobalInitializationSequenceTest
{
    [Fact]
    public void RunTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        var mandrel1 = Mock.Of<IActor>();
        var mandrel2 = Mock.Of<IActor>();
        var notSetActor = Mock.Of<IActor>();

        var globalInitializationSequence = new GlobalInitializationSequence(
            syncerActor,
            sequence =>
            {
                sequence.InitializeIfPossible(mandrel0);
                sequence.InitializeIfPossible(mandrel1);
                sequence.InitializeIfPossible(mandrel2);
                sequence.InitializeIfPossible(notSetActor);
                if (sequence.IsInitializingActors)
                    return;

                sequence.InitializeIfPossible(turret);
            }
        );
        globalInitializationSequence.SetInitializationState(
            Actor.Empty,
            InitializationStatus.Skipped
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel1,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel2,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.Run();
        Mock.Get(mandrel0)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(mandrel1)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(mandrel2)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_unReady")),
                Times.Never
            );
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );

        var action = () => globalInitializationSequence.Run();
        action.Should().Throw<PlatformException>();
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel1,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel2,
            InitializationStatus.Initialized
        );

        Mock.Get(mandrel0).Invocations.Clear();
        Mock.Get(mandrel1).Invocations.Clear();
        Mock.Get(mandrel2).Invocations.Clear();
        Mock.Get(turret).Invocations.Clear();
        globalInitializationSequence.Run();

        Mock.Get(mandrel0)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(mandrel1)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(mandrel2)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Never
            );
        Mock.Get(turret)
            .Verify(m => m.Send(It.Is<Message>(message => message.Name == "_unReady")), Times.Once);
        Mock.Get(turret)
            .Verify(
                m => m.Send(It.Is<Message>(message => message.Name == "_initialize")),
                Times.Once
            );
    }

    [Fact]
    public void IsCompleteTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        var mandrel1 = Mock.Of<IActor>();
        var mandrel2 = Mock.Of<IActor>();
        var globalInitializationSequence = new GlobalInitializationSequence(syncerActor, _ => { });
        globalInitializationSequence.SetInitializationState(
            Actor.Empty,
            InitializationStatus.Skipped
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(mandrel1, InitializationStatus.Skipped);
        globalInitializationSequence.SetInitializationState(mandrel2, InitializationStatus.Error);
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.IsComplete.Should().BeFalse();
        globalInitializationSequence.IsInitializingActors.Should().BeFalse();
        globalInitializationSequence.IsError.Should().BeTrue();

        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initializing
        );
        globalInitializationSequence.IsComplete.Should().BeFalse();
        globalInitializationSequence.IsInitializingActors.Should().BeTrue();
        globalInitializationSequence.IsError.Should().BeTrue();

        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Initialized
        );
        globalInitializationSequence.IsComplete.Should().BeTrue();
        globalInitializationSequence.IsInitializingActors.Should().BeFalse();
        globalInitializationSequence.IsError.Should().BeTrue();
    }

    [Fact]
    public void StateChangedTest()
    {
        var syncerActor = Mock.Of<IActor>();
        var turret = Mock.Of<IActor>();
        var mandrel0 = Mock.Of<IActor>();
        Mock.Get(turret).Setup(m => m.Name).Returns("turret");
        Mock.Get(mandrel0).Setup(m => m.Name).Returns("mandrel0");
        var globalInitializationSequence = new GlobalInitializationSequence(syncerActor, _ => { });

        var changedCalls = new List<(string actorName, InitializationStatus status)>();
        globalInitializationSequence.StateChanged += (sender, tuple) =>
        {
            changedCalls.Add(tuple);
        };
        globalInitializationSequence.SetInitializationState(
            turret,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Uninitialized
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initializing
        );
        globalInitializationSequence.SetInitializationState(
            mandrel0,
            InitializationStatus.Initialized
        );
        Assert.Equal(4, changedCalls.Count);
        Assert.Equal("turret", changedCalls[0].actorName);
        Assert.Equal(InitializationStatus.Uninitialized, changedCalls[0].status);
        Assert.Equal("mandrel0", changedCalls[1].actorName);
        Assert.Equal(InitializationStatus.Uninitialized, changedCalls[1].status);
        Assert.Equal("mandrel0", changedCalls[2].actorName);
        Assert.Equal(InitializationStatus.Initializing, changedCalls[2].status);
        Assert.Equal("mandrel0", changedCalls[3].actorName);
        Assert.Equal(InitializationStatus.Initialized, changedCalls[3].status);
    }
}
