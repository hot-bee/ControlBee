namespace ControlBee.Interfaces;

public interface ICounter : IDeviceChannel
{
    void SetCounterValue(double value);
    double GetCounterValue();
}
