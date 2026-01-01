using ControlBee.Constants;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DialogContext : IDialogContext
{
    public string ActorName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public int? Code { get; set; }
    public DialogSeverity Severity { get; set; } = DialogSeverity.Error;
    public string[] ActionButtons { get; set; } = [];
    public event EventHandler? CloseRequested;

    public void Close()
    {
        OnCloseRequested();
    }

    protected virtual void OnCloseRequested()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
