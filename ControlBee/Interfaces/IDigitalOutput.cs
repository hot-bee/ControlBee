namespace ControlBee.Interfaces;

public interface IDigitalOutput : IDigitalIO
{
    bool On { get; set; }
    bool Off { get; set; }
}
