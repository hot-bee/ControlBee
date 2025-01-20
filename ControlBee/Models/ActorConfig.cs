using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    IAxisFactory axisFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IVariableManager variableManager,
    ITimeManager timeManager
)
{
    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
}
