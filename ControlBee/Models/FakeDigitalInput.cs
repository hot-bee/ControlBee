using System.Reflection;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalInput(
    SystemConfigurations systemConfigurations,
    IScenarioFlowTester flowTester
) : DigitalInput(EmptyDeviceManager.Instance)
{
    private static readonly ILog Logger = LogManager.GetLogger(
        MethodBase.GetCurrentMethod()!.DeclaringType!
    );

    private readonly bool _skipWaitSensor = systemConfigurations.SkipWaitSensor;

    public bool On
    {
        set => InternalIsOn = value;
    }

    public bool Off
    {
        set => On = !value;
    }

    protected override void WaitSensor(bool isOn, int millisecondsTimeout)
    {
        if (_skipWaitSensor)
        {
            On = isOn;
            return;
        }

        base.WaitSensor(isOn, millisecondsTimeout);
    }

    protected override bool IsOnOffOrSet(bool on)
    {
        if (_skipWaitSensor)
        {
            On = on;
            return true;
        }

        return base.IsOnOffOrSet(on);
    }

    protected override void OnAfterSleepWaitingSensor()
    {
        flowTester.OnCheckpoint();
    }

    protected override void ReadFromDevice()
    {
        // Too verbose
        //Logger.Debug($"Digital Input: {ItemPath}={InternalIsOn}");
    }
}
