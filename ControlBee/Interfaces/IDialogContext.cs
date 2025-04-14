using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IDialogContext
{
    string ActorName { get; set; }
    string Name { get; set; }
    string Desc { get; set; }
    int? Code { get; set; }
    DialogSeverity Severity { get; set; }
    string[] ActionButtons { get; set; }
}
