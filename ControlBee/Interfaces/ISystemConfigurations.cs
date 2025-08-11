namespace ControlBee.Interfaces;

public interface ISystemConfigurations
{
    public bool FakeMode { get; }
    public bool FakeVision { get; }
    public bool SkipWaitSensor { get; }
    public bool TimeEmulationMode { get; }
    public string Version { get; set; }
    public void Save();
    public void Load();
}