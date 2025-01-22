using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyScenarioFlowTester : IScenarioFlowTester
{
    public static EmptyScenarioFlowTester Instance = new();

    public EmptyScenarioFlowTester() { }

    public void OnCheckpoint()
    {
        // Empty
    }
}
