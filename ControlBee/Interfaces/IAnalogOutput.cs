namespace ControlBee.Interfaces;

public interface IAnalogOutput : IAnalogIO
{
    void Write(long data);
    long Read();
    double ReadDouble();
    void WriteDouble(double data);
}
