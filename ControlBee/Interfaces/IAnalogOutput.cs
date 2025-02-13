namespace ControlBee.Interfaces;

public interface IAnalogOutput : IAnalogIO
{
    void Write(long data);
    long Read();
}
