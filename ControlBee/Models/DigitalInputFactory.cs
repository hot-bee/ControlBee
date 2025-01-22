using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalInputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IScenarioFlowTester scenarioFlowTester
) : IDigitalInputFactory
{
    public IDigitalInput Create()
    {
        return systemConfigurations.FakeMode
            ? new FakeDigitalInput(systemConfigurations, scenarioFlowTester)
            : new DigitalInput(deviceManager);
    }
}
