namespace ControlBee.Constants;

public class EventMessage
{
    public DateTime EventTime { get; set; }
    public string ActorName { get; set; } = null!;
    public string ItemPath { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DialogSeverity Severity { get; set; } = DialogSeverity.Info;
    public int? Code { get; set; }
    public string? Desc { get; set; }
}
