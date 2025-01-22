using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    IAxisFactory axisFactory,
    IDigitalInputFactory digitalInputFactory,
    IDigitalOutputFactory digitalOutputFactory,
    IVariableManager variableManager,
    ITimeManager timeManager,
    IActor? uiActor
)
{
    public ActorConfig(
        string actorName,
        IAxisFactory axisFactory,
        IDigitalInputFactory digitalInputFactory,
        IDigitalOutputFactory digitalOutputFactory,
        IVariableManager variableManager,
        ITimeManager timeManager
    )
        : this(
            actorName,
            axisFactory,
            digitalInputFactory,
            digitalOutputFactory,
            variableManager,
            timeManager,
            null
        ) { }

    public string ActorName => actorName;
    public IVariableManager VariableManager => variableManager;
    public ITimeManager TimeManager => timeManager;
    public IAxisFactory AxisFactory => axisFactory;
    public IDigitalInputFactory DigitalInputFactory => digitalInputFactory;
    public IDigitalOutputFactory DigitalOutputFactory => digitalOutputFactory;
    public IActor? UiActor { get; } = uiActor;
}
