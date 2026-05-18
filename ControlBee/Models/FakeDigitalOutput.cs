using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalOutput(ISystemConfigurations systemConfigurations, ITimeManager timeManager)
    : DigitalOutput(systemConfigurations, EmptyDeviceManager.Instance, timeManager)
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(DigitalOutput));

    public override void SetOn(bool on)
    {
        SetOnImpl(on);
    }
}
