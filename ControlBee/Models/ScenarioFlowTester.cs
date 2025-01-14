using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ScenarioFlowTester : IScenarioFlowTester
{
    private int _stepIndex;

    private ISimulationStep[]? _steps;
    public bool Complete => _stepIndex == _steps?.Length;

    void IScenarioFlowTester.OnCheckpoint()
    {
        while (_stepIndex < _steps?.Length)
        {
            var step = _steps[_stepIndex];
            switch (step)
            {
                case ConditionStep conditionStep:
                    if (conditionStep.Invoke())
                        _stepIndex++;
                    else
                        return;
                    break;
                case BehaviorStep behaviorsStep:
                    _stepIndex++;
                    behaviorsStep.Invoke();
                    break;
                default:
                    throw new ValueError();
            }
        }
    }

    public void Setup(ISimulationStep[] steps)
    {
        _steps = steps;
    }
}
