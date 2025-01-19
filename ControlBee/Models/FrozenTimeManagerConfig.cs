namespace ControlBee.Models;

public class FrozenTimeManagerConfig
{
    private const int DefaultTickMilliseconds = 100;
    public bool EmulationMode { get; set; }
    public bool ManualMode { get; set; }
    public int TickMilliseconds { get; set; } = DefaultTickMilliseconds;
}
