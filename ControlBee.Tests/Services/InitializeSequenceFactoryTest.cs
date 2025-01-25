using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Sequences;
using ControlBee.Services;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Services;

[TestSubject(typeof(InitializeSequenceFactory))]
public class InitializeSequenceFactoryTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateTest(bool fakeMode)
    {
        var systemConfigurations = new SystemConfigurations() { FakeMode = fakeMode };
        var initializeSequenceFactory = new InitializeSequenceFactory(systemConfigurations);
        var sequence = initializeSequenceFactory.Create(
            Mock.Of<IAxis>(),
            new SpeedProfile(),
            new Position1D()
        );
        if (fakeMode)
            Assert.IsType<FakeInitializeSequence>(sequence);
        else
            Assert.IsType<InitializeSequence>(sequence);
    }
}
