using ControlBee.Interfaces;
using ControlBee.Models;

namespace ControlBee.Tests.TestUtils;

public class ActorFactoryBaseConfig
{
    public SystemConfigurations? SystemConfigurations;
    public IActorFactory? ActorFactory;
    public IActorRegistry? ActorRegistry;
    public ISystemPropertiesDataSource? SystemPropertiesDataSource;
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
    public IAnalogInputFactory? AnalogInputFactory;
    public IAnalogOutputFactory? AnalogOutputFactory;
    public IDialogFactory? DialogFactory;
}
