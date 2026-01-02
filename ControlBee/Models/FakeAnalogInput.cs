namespace ControlBee.Models;

public class FakeAnalogInput() : AnalogInput(EmptyDeviceManager.Instance)
{
    public long Data
    {
        set => InternalData = value;
    }

    public override long Read()
    {
        return (long)InternalData;
    }
}
