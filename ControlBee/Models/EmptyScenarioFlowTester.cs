using ControlBee.Interfaces;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Models;

public class EmptyScenarioFlowTester : IScenarioFlowTester
{
    public static EmptyScenarioFlowTester Instance = new();

    public EmptyScenarioFlowTester() { }

    public void OnCheckpoint()
    {
        // Empty
    }

    public void Setup(ISimulationStep[][] stepGroups)
    {
        throw new NotImplementedException();
    }

    public bool Complete => throw new UnimplementedByDesignError();
}
