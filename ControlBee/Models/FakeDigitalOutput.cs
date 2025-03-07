using System.Reflection;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalOutput(IDeviceManager deviceManager, ITimeManager timeManager)
    : DigitalOutput(deviceManager, timeManager)
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalOutput));

    public override void WriteToDevice()
    {
        Logger.Debug($"Digital Output: {ItemPath}={InternalOn}");
    }
}
