using log4net;
using Newtonsoft.Json;

namespace ControlBee.Models;

public class SystemConfigurations
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(SystemConfigurations));
    public bool FakeMode { get; set; } // TODO: setter should be removed
    public bool FakeVision { get; set; } // TODO: setter should be removed
    public bool SkipWaitSensor { get; set; } // TODO: setter should be removed
    public bool TimeEmulationMode { get; set; } // TODO: setter should be removed

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
