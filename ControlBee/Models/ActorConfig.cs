using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ActorConfig
{
    public ActorConfig(string actorName, IVariableManager? variableManager = null)
    {
        ActorName = actorName;
        VariableManager = variableManager;
    }

    public string ActorName { get; }
    public IVariableManager? VariableManager { get; }
}
