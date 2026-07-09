using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class BehaviorStep(
    Action behavior,
    [CallerFilePath] string? callerFilePath = null,
    [CallerLineNumber] int callerLineNumber = 0
) : ISimulationStep
{
    public string Location => $"{Path.GetFileName(callerFilePath)}:{callerLineNumber}";

    public void Invoke()
    {
        behavior.Invoke();
    }
}
