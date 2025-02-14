using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DigitalInputFactory(
    SystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IScenarioFlowTester scenarioFlowTester,
    IDeviceMonitor deviceMonitor
) : IDigitalInputFactory
{
    public IDigitalInput Create()
    {
        var input = systemConfigurations.FakeMode
            ? new FakeDigitalInput(systemConfigurations, scenarioFlowTester)
            : new DigitalInput(deviceManager);
        deviceMonitor.Add(input);
        return input;
    }
}
