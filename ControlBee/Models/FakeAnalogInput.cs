namespace ControlBee.Models;

public class FakeAnalogInput() : AnalogInput(EmptyDeviceManager.Instance)
{
    public long Data
    {
        set => InternalData = value;
    }
}
