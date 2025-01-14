using ControlBee.Interfaces;

namespace ControlBee.Models;

public class BehaviorStep(Action behavior) : ISimulationStep
{
    public void Invoke()
    {
        behavior.Invoke();
    }
}
