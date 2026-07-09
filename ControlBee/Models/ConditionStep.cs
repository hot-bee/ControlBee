using System.Runtime.CompilerServices;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ConditionStep(
    Func<bool> condition,
    [CallerFilePath] string? callerFilePath = null,
    [CallerLineNumber] int callerLineNumber = 0
) : ISimulationStep
{
    public string Location => $"{Path.GetFileName(callerFilePath)}:{callerLineNumber}";

    public bool Invoke()
    {
        return condition.Invoke();
    }
}
