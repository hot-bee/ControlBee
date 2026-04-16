namespace ControlBee.Models;

public class FakeCounter() : Counter(EmptyDeviceManager.Instance)
{
    public int Count
    {
        set => SetCounterValueImpl(value);
    }

    public override void SetCounterValue(double value)
    {
        SetCounterValueImpl(value);
    }

    public override double GetCounterValue()
    {
        return base.GetCounterValue();
    }
}
