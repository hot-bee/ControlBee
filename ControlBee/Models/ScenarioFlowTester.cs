using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;
using log4net;

namespace ControlBee.Models;

public class ScenarioFlowTester : IScenarioFlowTester
{
    private static readonly ILog Logger = LogManager.GetLogger("Sequence");

    private ISimulationStep[][]? _stepGroups;
    private int[]? _stepIndices;

    // Tracks the step index last logged as "waiting" per group, so a stuck ConditionStep logs
    // once instead of spamming every checkpoint tick. -1 means nothing is currently logged as stuck.
    private int[]? _loggedWaitIndices;

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
        if (_stepIndices == null || _stepGroups == null || _loggedWaitIndices == null)
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
                            {
                                Logger.Debug(
                                    $"Group {i} step {_stepIndices[i]} passed. ({conditionStep.Location})"
                                );
                                _stepIndices[i]++;
                                _loggedWaitIndices[i] = -1;
                            }
                            else
                            {
                                if (_loggedWaitIndices[i] != _stepIndices[i])
                                {
                                    _loggedWaitIndices[i] = _stepIndices[i];
                                    Logger.Debug(
                                        $"Group {i} waiting on step {_stepIndices[i]}. ({conditionStep.Location})"
                                    );
                                }
                                proceeded = false;
                            }
                            break;
                        case BehaviorStep behaviorsStep:
                            Logger.Debug(
                                $"Group {i} step {_stepIndices[i]} executing. ({behaviorsStep.Location})"
                            );
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
        _loggedWaitIndices = new int[_stepGroups.GetLength(0)];
        Array.Fill(_loggedWaitIndices, -1);
    }
}
