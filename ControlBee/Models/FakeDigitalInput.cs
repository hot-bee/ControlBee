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

    protected override void WaitSensor(bool isOn, int millisecondsTimeout)
    {
        if (_skipWaitSensor)
        {
            On = isOn;
            return;
        }
        base.WaitSensor(isOn, millisecondsTimeout);
    }

    protected override void OnAfterSleepWaitingSensor()
    {
        flowTester.OnCheckpoint();
    }

    public override void ReadFromDevice()
    {
        Logger.Debug($"Digital Input: {ItemPath}={InternalIsOn}");
    }
}
