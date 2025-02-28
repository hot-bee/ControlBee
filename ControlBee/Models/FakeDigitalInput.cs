using System.Reflection;
using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class FakeDigitalInput(
    SystemConfigurations systemConfigurations,
    IScenarioFlowTester flowTester
) : DigitalInput(EmptyDeviceManager.Instance)
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

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

    protected override bool IsOnOffOrValue(bool value)
    {
        return _skipWaitSensor ? value : base.IsOnOffOrValue(value);
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
