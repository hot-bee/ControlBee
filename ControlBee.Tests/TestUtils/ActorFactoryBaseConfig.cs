using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Services;

namespace ControlBee.Tests.TestUtils;

public class ActorFactoryBaseConfig
{
    public SystemConfigurations? SystemConfigurations;
    public IActorFactory? ActorFactory;
    public IActorRegistry? ActorRegistry;
    public IActorItemInjectionDataSource? ActorItemInjectionDataSource;
    public IInitializeSequenceFactory? InitializeSequenceFactory;
    public IDigitalOutputFactory? DigitalOutputFactory;
    public IDigitalInputFactory? DigitalInputFactory;
    public IBinaryActuatorFactory? BinaryActuatorFactory;
    public IVariableManager? VariableManager;
    public IAxisFactory? AxisFactory;
    public IScenarioFlowTester? ScenarioFlowTester;
    public ITimeManager? TimeManager;
    public IDeviceManager? DeviceManager;
    public IDatabase? Database;
}
