using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;

namespace ControlBee.Tests.TestUtils;

public class ActorFactoryBaseConfig
{
    public ActorFactory? ActorFactory;
    public ActorRegistry? ActorRegistry;
    public IActorItemInjectionDataSource? ActorItemInjectionDataSource;
    public InitializeSequenceFactory? InitializeSequenceFactory;
    public DigitalOutputFactory? DigitalOutputFactory;
    public DigitalInputFactory? DigitalInputFactory;
    public VariableManager? VariableManager;
    public AxisFactory? AxisFactory;
    public ScenarioFlowTester? ScenarioFlowTester;
    public FrozenTimeManager? TimeManager;
    public DeviceManager? DeviceManager;
    public SystemConfigurations? SystemConfigurations;
    public IDatabase? Database;
}
