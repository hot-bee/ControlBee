using ControlBee.Constants;

namespace ControlBee.Interfaces;

public interface IDialogContext
{
    string ActorName { get; set; }
    string ItemPath { get; set; }
    string Name { get; set; }
    string Desc { get; set; }
    int? Code { get; set; }
    DialogSeverity Severity { get; set; }
    string[] ActionButtons { get; set; }
    bool IsActive { get; set; } // TODO: Not sure context should have this property, maybe should be managed by DialogService instead
    event EventHandler? CloseRequested;
    void Close();
}
