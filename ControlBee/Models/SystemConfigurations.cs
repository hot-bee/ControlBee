using ControlBee.Interfaces;
using log4net;
using Newtonsoft.Json;

namespace ControlBee.Models;

public class SystemConfigurations : ISystemConfigurations
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(SystemConfigurations));
    public int VisionChannelCount { get; set; } = 0;

    public string RecipeName { get; set; } = "Default";

    public bool FakeMode { get; set; } = true; // TODO: setter should be removed
    public bool SkipWaitSensor { get; set; } // TODO: setter should be removed
    public bool TimeEmulationMode { get; set; } // TODO: setter should be removed
    public string DataFolder { get; set; } = "";
    public string Version { get; set; } = "0.0.0"; // TODO: Relocate this to somewhere
    public bool AutoVariableSave { get; set; } = true;
    public bool DevMode { get; set; } = true;
    public int AdminLevel { get; set; } = 0;

    public void Save()
    {
        var contents = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText("SystemConfig.json", contents);
    }

    public void Load()
    {
        try
        {
            var contents = File.ReadAllText("SystemConfig.json");
            JsonConvert.PopulateObject(contents, this);
        }
        catch (FileNotFoundException ex)
        {
            Logger.Warn("Couldn't find the saved config file.");
            Save();
        }
    }
}