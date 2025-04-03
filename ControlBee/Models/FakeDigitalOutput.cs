using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalOutput(ITimeManager timeManager)
    : DigitalOutput(EmptyDeviceManager.Instance, timeManager)
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalOutput));
}
