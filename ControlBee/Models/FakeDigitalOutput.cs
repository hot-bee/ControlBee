using System.Reflection;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalOutput() : DigitalOutput(EmptyDeviceManager.Instance)
{
    private static readonly ILog Logger = LogManager.GetLogger(
        MethodBase.GetCurrentMethod()!.DeclaringType!
    );

    public override void WriteToDevice()
    {
        Logger.Debug($"Digital Output: {ItemPath}={InternalOn}");
    }
}
