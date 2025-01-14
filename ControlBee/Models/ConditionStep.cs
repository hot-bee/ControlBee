using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ConditionStep(Func<bool> condition) : ISimulationStep
{
    public bool Invoke()
    {
        return condition.Invoke();
    }
}
