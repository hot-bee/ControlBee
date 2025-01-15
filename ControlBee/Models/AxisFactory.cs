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
        if (systemConfigurations.EmulationMode)
            return fakeAxisFactory.Create(systemConfigurations.EmulationMode);
        return new Axis(timeManager);
    }
}
