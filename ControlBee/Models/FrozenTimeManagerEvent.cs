namespace ControlBee.Models;

public class FrozenTimeManagerEvent
{
    public AutoResetEvent ResumedEvent = new(false);
    public AutoResetEvent ResumeEvent = new(false);
    public AutoResetEvent SleepEvent = new(false);
}
