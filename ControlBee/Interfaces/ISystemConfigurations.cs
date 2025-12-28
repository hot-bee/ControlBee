namespace ControlBee.Interfaces;

public interface ISystemConfigurations
{
    bool FakeMode { get; }
    bool SkipWaitSensor { get; }
    bool TimeEmulationMode { get; }
    string DataFolder { get; }
    string Version { get; set; }
    string RecipeName { get; set; }
    int VisionChannelCount { get; set; }
    bool AutoVariableSave { get; set; }
    bool IsTopLevelLogin { get; }
    int AdminLevel { get; set; }
    bool MonitorDigitalInputsByDevice { get; set; }
    void Save();
    void Load();
}