using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(
    string actorName,
    IVariableManager variableManager,
    ITimeManager timeManager
)
{
    public string ActorName { get; } = actorName;
    public IVariableManager VariableManager { get; } = variableManager;
    public ITimeManager TimeManager { get; set; } = timeManager;
}
