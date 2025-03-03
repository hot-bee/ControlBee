using System;
using ControlBee.Interfaces;
using ControlBee.Models;
using Moq;

namespace ControlBee.Tests.TestUtils;

public class MockActorFactory
{
    public static IActor Create(string name)
    {
        var actor = Mock.Of<IActor>();
        Mock.Get(actor).Setup(m => m.Name).Returns(name);
        Mock.Get(actor).Setup(m => m.Send(It.IsAny<Message>())).Returns(Guid.NewGuid());
        return actor;
    }
}
