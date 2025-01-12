using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig(string actorName, IVariableManager variableManager)
{
    public string ActorName { get; } = actorName;
    public IVariableManager VariableManager { get; } = variableManager;
}
