namespace ControlBee.Models;

public class FrozenTimeManagerEvent
{
    public volatile bool IsSleeping = false;
    public AutoResetEvent ResumedEvent = new(false);
    public AutoResetEvent ResumeEvent = new(false);
    public Thread Thread = Thread.CurrentThread;
}
