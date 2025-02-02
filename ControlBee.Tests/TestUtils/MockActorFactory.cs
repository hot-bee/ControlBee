using ControlBee.Interfaces;
using Moq;

namespace ControlBee.Tests.TestUtils
{
    public class MockActorFactory
    {
        public static IActor Create(string name)
        {
            var actor = Mock.Of<IActor>();
            Mock.Get(actor).Setup(m => m.Name).Returns(name);
            return actor;
        }
    }
}
