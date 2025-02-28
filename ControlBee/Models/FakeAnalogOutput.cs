using System.Reflection;
using log4net;

namespace ControlBee.Models;

public class FakeAnalogOutput() : AnalogOutput(EmptyDeviceManager.Instance)
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    protected override void WriteToDevice()
    {
        Logger.Debug($"Digital Output: {ItemPath}={InternalData}");
    }
}
