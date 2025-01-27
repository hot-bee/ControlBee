namespace ControlBee.Interfaces;

public interface IScenarioFlowTester
{
    void OnCheckpoint();
    void Setup(ISimulationStep[][] stepGroups);
    bool Complete { get; }
}
