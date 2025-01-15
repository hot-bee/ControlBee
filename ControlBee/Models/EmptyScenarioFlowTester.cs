using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyScenarioFlowTester : IScenarioFlowTester
{
    public void OnCheckpoint()
    {
        // Empty
    }
}
