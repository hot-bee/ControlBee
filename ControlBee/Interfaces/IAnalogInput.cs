namespace ControlBee.Interfaces;

public interface IAnalogInput : IAnalogIO
{
    long Read();
    double ReadDouble();
}
