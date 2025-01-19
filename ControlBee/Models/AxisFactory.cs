using ControlBee.Interfaces;

namespace ControlBee.Models;

public class AxisFactory(
    SystemConfigurations systemConfigurations,
    IFrozenTimeManager timeManager,
    IFakeAxisFactory fakeAxisFactory
) : IAxisFactory
{
    public IAxis Create()
    {
        if (systemConfigurations.FakeMode)
            return fakeAxisFactory.Create(systemConfigurations.SkipWaitSensor);
        return new Axis(timeManager);
    }
}
