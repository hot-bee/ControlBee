using ControlBee.Interfaces;

namespace ControlBee.Models;

public class VisionFactory(
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    ITimeManager timeManager
) : IVisionFactory
{
    public IVision Create()
    {
        var vision = systemConfigurations.FakeVision
            ? new FakeVision(deviceManager, timeManager)
            : new Vision(deviceManager, timeManager);
        return vision;
    }
}
