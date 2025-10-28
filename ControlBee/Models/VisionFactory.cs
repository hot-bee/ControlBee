using ControlBee.Interfaces;

namespace ControlBee.Models;

public class VisionFactory(
    IDeviceManager deviceManager,
    ITimeManager timeManager
) : IVisionFactory
{
    public IVision Create()
    {
        var vision = new Vision(deviceManager, timeManager);
        return vision;
    }
}
