using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class ScenarioFlowTester : IScenarioFlowTester
{
    private ISimulationStep[][]? _stepGroups;
    private int[]? _stepIndices;

    public bool Complete
    {
        get
        {
            if (_stepIndices == null || _stepGroups == null)
                return false;
            for (var i = 0; i < _stepIndices.Length; i++)
                if (_stepIndices[i] < _stepGroups[i].Length)
                    return false;
            return true;
        }
    }

    void IScenarioFlowTester.OnCheckpoint()
    {
        if (_stepIndices == null || _stepGroups == null)
            return;
        lock (this)
        {
            for (var i = 0; i < _stepIndices.Length; i++)
            {
                var stepGroup = _stepGroups[i];
                while (_stepIndices[i] < stepGroup.Length)
                {
                    var step = stepGroup[_stepIndices[i]];
                    var proceeded = true;
                    switch (step)
                    {
                        case ConditionStep conditionStep:
                            if (conditionStep.Invoke())
                                _stepIndices[i]++;
                            else
                                proceeded = false;
                            break;
                        case BehaviorStep behaviorsStep:
                            _stepIndices[i]++;
                            behaviorsStep.Invoke();
                            break;
                        default:
                            throw new ValueError();
                    }

                    if (!proceeded)
                        break;
                }
            }
        }
    }

    public void Setup(ISimulationStep[][] stepGroups)
    {
        _stepGroups = stepGroups;
        _stepIndices = new int[_stepGroups.GetLength(0)];
    }
}
